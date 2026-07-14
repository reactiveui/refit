// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>Resolves the HTTP verb from a method's <c>[Get]</c>/<c>[Post]</c>/custom HTTP method attribute.</content>
internal static partial class Parser
{
    /// <summary>Maps a built-in Refit HTTP method attribute's metadata name to its HTTP verb.</summary>
    /// <param name="attributeMetadataName">The attribute type's metadata name, for example <c>GetAttribute</c>.</param>
    /// <returns>The HTTP verb, or <see langword="null"/> for an attribute that is not one of Refit's built-in verbs.</returns>
    [ExcludeFromCodeCoverage]
    internal static string? MapKnownHttpVerb(string attributeMetadataName) =>
        attributeMetadataName switch
        {
            "DeleteAttribute" => "DELETE",
            "GetAttribute" => "GET",
            "HeadAttribute" => "HEAD",
            "OptionsAttribute" => "OPTIONS",
            "PatchAttribute" => "PATCH",
            "PostAttribute" => "POST",
            "PutAttribute" => "PUT",
            _ => null
        };

    /// <summary>Gets the HTTP method name represented by a Refit method attribute.</summary>
    /// <param name="attributeClass">The resolved HTTP method attribute type.</param>
    /// <returns>The HTTP method name, or an empty string when a custom attribute's verb is not statically readable.</returns>
    private static string GetHttpMethodName(INamedTypeSymbol attributeClass) =>
        MapKnownHttpVerb(attributeClass.MetadataName) ?? ResolveCustomHttpVerb(attributeClass);

    /// <summary>Resolves a statically-readable custom HTTP verb, or an empty string when the method must fall back.</summary>
    /// <param name="attributeClass">The custom HTTP method attribute type.</param>
    /// <returns>The verb, or an empty string.</returns>
    private static string ResolveCustomHttpVerb(INamedTypeSymbol attributeClass) =>
        TryResolveCustomHttpVerb(attributeClass, out var verb) ? verb : string.Empty;

    /// <summary>Reads a custom HTTP verb from a derived attribute whose <c>Method</c> getter returns a string literal.</summary>
    /// <param name="attributeClass">The custom HTTP method attribute type.</param>
    /// <param name="verb">The resolved verb when the getter is a recognizable literal.</param>
    /// <returns><see langword="true"/> when the verb is statically readable.</returns>
    /// <remarks>
    /// The verb is otherwise an arbitrary runtime value (<c>HttpMethodAttribute.Method</c> is abstract), so only a getter
    /// that constructs an <c>HttpMethod</c> from a string literal - the one shape the generator can evaluate - is inlined;
    /// any other override keeps using the reflection request builder. Resolution is syntax and symbol only (no semantic
    /// model), so the RF006 analyzer and the generator agree. The reflection builder reads the same verb at runtime, so
    /// the emitted <c>new HttpMethod("VERB")</c> matches it.
    /// </remarks>
    private static bool TryResolveCustomHttpVerb(INamedTypeSymbol attributeClass, out string verb)
    {
        verb = string.Empty;

        // The property type pins this to the real HttpMethod override rather than an unrelated "Method", and any
        // non-HttpMethod or unreadable override falls through.
        var (property, getter) = FindMethodPropertyGetter(attributeClass);
        if (property.Type.ToDisplayString() != "System.Net.Http.HttpMethod")
        {
            return false;
        }

        foreach (var reference in getter.DeclaringSyntaxReferences)
        {
            if (GetterReturnExpression(reference.GetSyntax()) is ObjectCreationExpressionSyntax { ArgumentList.Arguments: [var argument] }
                && argument.Expression is LiteralExpressionSyntax { Token.Value: string literal }
                && literal.Length > 0)
            {
                verb = literal;
                return true;
            }
        }

        return false;
    }

    /// <summary>Finds the most-derived <c>Method</c> property and its getter on an attribute type.</summary>
    /// <param name="attributeClass">The custom HTTP method attribute type.</param>
    /// <returns>The <c>Method</c> property and its getter.</returns>
    /// <remarks>Walks the attribute and its bases for the most-derived <c>Method</c> override. Every attribute reaching
    /// here derives from HttpMethodAttribute, which declares an (abstract) HttpMethod Method getter, so the walk always
    /// resolves one and the trailing fallback is unreachable.</remarks>
    [ExcludeFromCodeCoverage]
    private static (IPropertySymbol Property, IMethodSymbol Getter) FindMethodPropertyGetter(INamedTypeSymbol attributeClass)
    {
        for (INamedTypeSymbol? current = attributeClass; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers("Method"))
            {
                if (member is IPropertySymbol { GetMethod: { } getter } candidate)
                {
                    return (candidate, getter);
                }
            }
        }

        return default!;
    }

    /// <summary>Gets the expression a property getter returns, as an expression body or a single return statement.</summary>
    /// <param name="syntax">The getter or property syntax.</param>
    /// <returns>The returned expression, or null when the getter is not a single expression.</returns>
    [ExcludeFromCodeCoverage]
    private static ExpressionSyntax? GetterReturnExpression(SyntaxNode syntax) =>
        syntax switch
        {
            ArrowExpressionClauseSyntax { Expression: { } arrowBody } => arrowBody,
            AccessorDeclarationSyntax { ExpressionBody.Expression: { } accessorBody } => accessorBody,
            AccessorDeclarationSyntax { Body.Statements: [ReturnStatementSyntax { Expression: { } returned }] } => returned,
            _ => null
        };
}
