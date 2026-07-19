// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>
/// Builds the assembly-scoped names of the generated helpers. Folding the assembly name into the container type and
/// the internal namespace gives each assembly distinctly named copies, so two assemblies linked by
/// <c>[InternalsVisibleTo]</c> no longer emit identically named internal types that the second compilation sees twice.
/// </content>
internal static partial class Parser
{
    /// <summary>Builds the internal generated namespace from a consumer-provided namespace prefix and the assembly name.</summary>
    /// <param name="refitInternalNamespace">The optional user or MSBuild-supplied namespace prefix.</param>
    /// <param name="assemblyName">The compilation assembly name, folded in so each assembly gets a distinct namespace.</param>
    /// <returns>A valid C# namespace for generated Refit internals.</returns>
    internal static string BuildRefitInternalNamespace(string? refitInternalNamespace, string? assemblyName)
    {
        var prefixedNamespace = string.IsNullOrWhiteSpace(refitInternalNamespace)
            ? RefitInternalGeneratedSuffix
            : refitInternalNamespace + RefitInternalGeneratedSuffix;

        // Fold the assembly name in as a trailing segment so the PreserveAttribute this namespace houses no longer
        // collides across assemblies linked by [InternalsVisibleTo]. Blank names (unnamed compilations) leave the
        // namespace as-is, matching the historical single-assembly output.
        var assemblyScope = SanitizeAssemblyScope(assemblyName);
        var rawNamespace = assemblyScope.Length == 0
            ? prefixedNamespace
            : prefixedNamespace + "." + assemblyScope;
        var parts = rawNamespace.Split('.');
        var builder = new StringBuilder(rawNamespace.Length);

        foreach (var part in parts)
        {
            var normalized = NormalizeNamespacePart(part);
            if (normalized.Length == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                _ = builder.Append('.');
            }

            _ = builder.Append(normalized);
        }

        // The raw namespace always contains the non-empty RefitInternalGeneratedSuffix segment, so at least one
        // normalized part is appended and the builder is never empty here.
        return builder.ToString();
    }

    /// <summary>Builds the generated implementation container name for the compilation's assembly.</summary>
    /// <param name="assemblyName">The compilation assembly name, or <see langword="null"/> when unavailable.</param>
    /// <returns>The container name, scoped to the assembly so each assembly emits a distinct container.</returns>
    internal static string BuildGeneratedContainerName(string? assemblyName) =>
        GeneratedContainerBaseName + SanitizeAssemblyScope(assemblyName);

    /// <summary>Reduces an assembly name to an identifier fragment folded into generated names.</summary>
    /// <param name="assemblyName">The compilation assembly name, or <see langword="null"/> when unavailable.</param>
    /// <returns>The fragment, or an empty string when the assembly name is null or blank. This must stay identical to
    /// the runtime <c>UniqueName.SanitizeAssemblyName</c> so the reflection lookup reconstructs the same container.</returns>
    internal static string SanitizeAssemblyScope(string? assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(assemblyName!.Length);
        foreach (var character in assemblyName)
        {
            _ = builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.ToString();
    }

    /// <summary>Normalizes one namespace segment into a valid identifier.</summary>
    /// <param name="part">The namespace segment.</param>
    /// <returns>The normalized segment, or an empty string when the segment is blank.</returns>
    internal static string NormalizeNamespacePart(string part)
    {
        if (string.IsNullOrWhiteSpace(part))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(part.Length + 1);
        foreach (var character in part)
        {
            if (builder.Length == 0)
            {
                if (SyntaxFacts.IsIdentifierStartCharacter(character))
                {
                    _ = builder.Append(character);
                }
                else if (SyntaxFacts.IsIdentifierPartCharacter(character))
                {
                    _ = builder.Append('_').Append(character);
                }
                else
                {
                    _ = builder.Append('_');
                }

                continue;
            }

            _ = builder.Append(SyntaxFacts.IsIdentifierPartCharacter(character) ? character : '_');
        }

        var normalized = builder.ToString();
        return SyntaxFacts.GetKeywordKind(normalized) != SyntaxKind.None
            || SyntaxFacts.GetContextualKeywordKind(normalized) != SyntaxKind.None
            ? "_" + normalized
            : normalized;
    }
}
