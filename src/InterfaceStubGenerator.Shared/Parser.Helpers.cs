// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Generator;

/// <summary>Internal parser helpers that are directly covered by focused tests.</summary>
internal static partial class Parser
{
    /// <summary>The length of the <c>&lt;&gt;</c> pair wrapping a generic type parameter list.</summary>
    private const int GenericBracketLength = 2;

    /// <summary>The assumed rendered length of one type parameter, used only to size a <see cref="StringBuilder"/>.</summary>
    private const int EstimatedTypeParameterLength = 32;

    /// <summary>Builds the unqualified declared method name, including any generic type parameters.</summary>
    /// <param name="methodSymbol">The method symbol.</param>
    /// <returns>The declared method name without its interface qualifier.</returns>
    internal static string BuildDeclaredBaseName(IMethodSymbol methodSymbol)
    {
        // Keep the declared method name unqualified.
        var declaredBaseName = methodSymbol.Name;
        var lastDot = declaredBaseName.LastIndexOf('.');
        if (lastDot >= 0)
        {
            declaredBaseName = declaredBaseName[(lastDot + 1)..];
        }

        // A method named after a C# keyword (e.g. @class) must be emitted verbatim, otherwise the generated
        // signature is invalid C#. Escape the name before any generic type-parameter list is appended.
        declaredBaseName = EscapeIdentifier(declaredBaseName);

        if (methodSymbol.TypeParameters.IsEmpty)
        {
            return declaredBaseName;
        }

        var typeParameters = methodSymbol.TypeParameters;
        var estimatedCapacity =
            declaredBaseName.Length + GenericBracketLength + (typeParameters.Length * EstimatedTypeParameterLength);
        var builder = new StringBuilder(estimatedCapacity)
            .Append(declaredBaseName)
            .Append('<');

        for (var i = 0; i < typeParameters.Length; i++)
        {
            if (i > 0)
            {
                _ = builder.Append(", ");
            }

            _ = builder.Append(typeParameters[i].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        _ = builder.Append('>');
        return builder.ToString();
    }

    /// <summary>Prefixes an identifier with <c>@</c> when it is a reserved C# keyword, so it can be emitted verbatim
    /// as a declaration name. Non-keyword identifiers (including contextual keywords, which are valid as identifiers)
    /// are returned unchanged so the generated output for ordinary members is unaffected.</summary>
    /// <param name="identifier">The simple identifier to escape.</param>
    /// <returns>The identifier, prefixed with <c>@</c> when it is a reserved keyword; otherwise the identifier itself.</returns>
    internal static string EscapeIdentifier(string identifier) =>
        SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ? "@" + identifier : identifier;

    /// <summary>Determines whether a method is decorated with a Refit HTTP method attribute.</summary>
    /// <param name="methodSymbol">The method symbol to inspect.</param>
    /// <param name="httpMethodAttribute">The Refit HTTP method attribute symbol.</param>
    /// <returns><see langword="true"/> if the method is a Refit method; otherwise, <see langword="false"/>.</returns>
    internal static bool IsRefitMethod(IMethodSymbol? methodSymbol, INamedTypeSymbol httpMethodAttribute)
    {
        if (methodSymbol is null)
        {
            return false;
        }

        // Avoid LINQ here: this is called for every candidate method and every inherited member.
        foreach (var attributeData in methodSymbol.GetAttributes())
        {
            if (attributeData.AttributeClass!.InheritsFromOrEquals(httpMethodAttribute))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether any base interface declares a Refit method.</summary>
    /// <param name="interfaceSymbol">The interface symbol to inspect.</param>
    /// <param name="httpMethodAttribute">The Refit HTTP method attribute symbol.</param>
    /// <returns><see langword="true"/> if a base interface declares a Refit method; otherwise, <see langword="false"/>.</returns>
    internal static bool HasDerivedRefitMethods(
        INamedTypeSymbol interfaceSymbol,
        INamedTypeSymbol httpMethodAttribute)
    {
        foreach (var baseInterface in interfaceSymbol.AllInterfaces)
        {
            foreach (var member in baseInterface.GetMembers())
            {
                if (member is IMethodSymbol method && IsRefitMethod(method, httpMethodAttribute))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
