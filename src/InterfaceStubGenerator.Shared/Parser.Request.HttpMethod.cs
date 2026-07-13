// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>Resolves the HTTP verb from a method's <c>[Get]</c>/<c>[Post]</c>/custom HTTP method attribute.</content>
internal static partial class Parser
{
    /// <summary>Gets the HTTP method name represented by a Refit method attribute.</summary>
    /// <param name="attributeClass">The attribute type.</param>
    /// <returns>The HTTP method name, or an empty string when a custom attribute's verb is not statically readable.</returns>
    private static string GetHttpMethodName(INamedTypeSymbol? attributeClass) =>
        (attributeClass?.MetadataName switch
        {
            "DeleteAttribute" => "DELETE",
            "GetAttribute" => "GET",
            "HeadAttribute" => "HEAD",
            "OptionsAttribute" => "OPTIONS",
            "PatchAttribute" => "PATCH",
            "PostAttribute" => "POST",
            "PutAttribute" => "PUT",
            _ => (string?)null
        })
        ?? ResolveCustomHttpVerb(attributeClass);

    /// <summary>Resolves a statically-readable custom HTTP verb, or an empty string when the method must fall back.</summary>
    /// <param name="attributeClass">The custom HTTP method attribute type, or null.</param>
    /// <returns>The verb, or an empty string.</returns>
    private static string ResolveCustomHttpVerb(INamedTypeSymbol? attributeClass) =>
        attributeClass is not null && TryResolveCustomHttpVerb(attributeClass, out var verb)
            ? verb
            : string.Empty;

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

        // The property type pins this to the real HttpMethodAttribute.Method override rather than an unrelated "Method".
        if (FindHttpMethodProperty(attributeClass) is not { GetMethod: { } getter } property
            || property.Type.ToDisplayString() != "System.Net.Http.HttpMethod")
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

    /// <summary>Finds the most-derived <c>Method</c> property declared on an attribute type or its bases.</summary>
    /// <param name="attributeClass">The attribute type.</param>
    /// <returns>The property, or null when none is found.</returns>
    private static IPropertySymbol? FindHttpMethodProperty(INamedTypeSymbol attributeClass)
    {
        for (INamedTypeSymbol? current = attributeClass; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers("Method"))
            {
                if (member is IPropertySymbol { GetMethod: not null } property)
                {
                    return property;
                }
            }
        }

        return null;
    }

    /// <summary>Gets the expression a property getter returns, as an expression body or a single return statement.</summary>
    /// <param name="syntax">The getter or property syntax.</param>
    /// <returns>The returned expression, or null when the getter is not a single expression.</returns>
    private static ExpressionSyntax? GetterReturnExpression(SyntaxNode syntax) =>
        syntax switch
        {
            PropertyDeclarationSyntax { ExpressionBody.Expression: { } propertyBody } => propertyBody,
            ArrowExpressionClauseSyntax { Expression: { } arrowBody } => arrowBody,
            AccessorDeclarationSyntax { ExpressionBody.Expression: { } accessorBody } => accessorBody,
            AccessorDeclarationSyntax { Body.Statements: [ReturnStatementSyntax { Expression: { } returned }] } => returned,
            _ => null
        };
}
