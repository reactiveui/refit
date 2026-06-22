// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text;
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Internal parser helpers that are directly covered by focused tests.</summary>
internal static partial class Parser
{
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

        if (methodSymbol.TypeParameters.Length == 0)
        {
            return declaredBaseName;
        }

        var typeParameters = methodSymbol.TypeParameters;
        var estimatedCapacity = declaredBaseName.Length + 2 + (typeParameters.Length * 32);
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
            if (attributeData.AttributeClass?.InheritsFromOrEquals(httpMethodAttribute) == true)
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
