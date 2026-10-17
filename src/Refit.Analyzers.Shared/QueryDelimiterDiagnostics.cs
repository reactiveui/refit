// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Refit.Generator;

namespace Refit.Analyzers;

/// <summary>Identifies positional query delimiters that look like intended aliases.</summary>
internal static class QueryDelimiterDiagnostics
{
    /// <summary>Reports delimiter mistakes on a Refit method.</summary>
    /// <param name="method">The method to inspect.</param>
    /// <param name="formattable">The resolved scalar formatting interface.</param>
    /// <param name="report">The diagnostic callback.</param>
    /// <param name="cancellationToken">The analysis cancellation token.</param>
    internal static void Report(
        IMethodSymbol method,
        INamedTypeSymbol? formattable,
        Action<Diagnostic> report,
        CancellationToken cancellationToken)
    {
        foreach (var parameter in method.Parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var attribute = Generator.QueryParameterParsing.FindParameterAttribute(parameter, "QueryAttribute");
            if (attribute is null
                || attribute.ConstructorArguments.Length != 1
                || attribute.ConstructorArguments[0].Value is not string { Length: > 0 } delimiter
                || Generator.QueryParameterParsing.FindParameterAttribute(parameter, "AliasAsAttribute") is not null
                || HasExplicitName(attribute))
            {
                continue;
            }

            if (!Generator.RequestParameterKindParsing.IsSimpleType(parameter.Type, formattable) && !LooksLikeName(delimiter))
            {
                continue;
            }

            report(Diagnostic.Create(
                DiagnosticDescriptors.QueryDelimiterAsAlias,
                attribute.ApplicationSyntaxReference!.GetSyntax(cancellationToken).GetLocation(),
                delimiter,
                parameter.Name));
        }
    }

    /// <summary>Checks whether a query attribute already specifies its name.</summary>
    /// <param name="attribute">The query attribute.</param>
    /// <returns>Whether an explicit name is present.</returns>
    private static bool HasExplicitName(AttributeData attribute)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == "Name" && argument.Value.Value is string)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Checks identifier-shaped strings while excluding punctuation-only delimiters.</summary>
    /// <param name="delimiter">The positional delimiter.</param>
    /// <returns>Whether the delimiter resembles a query name.</returns>
    private static bool LooksLikeName(string delimiter)
    {
        if (delimiter.Length <= 1)
        {
            return false;
        }

        var hasLetter = false;
        foreach (var character in delimiter)
        {
            if (char.IsLetter(character))
            {
                hasLetter = true;
            }
            else if (!char.IsDigit(character) && character is not '_' and not '-')
            {
                return false;
            }
        }

        return hasLetter;
    }
}
