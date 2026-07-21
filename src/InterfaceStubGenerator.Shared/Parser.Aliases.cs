// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>Extern-alias-aware type qualification, so generated code compiles against types that are only reachable
/// through an <c>extern alias</c> (for example a type that collides with a global one).</content>
internal static partial class Parser
{
    /// <summary>The fully-qualified name of a named type without its generic arguments or the <c>global::</c> prefix.</summary>
    private static readonly SymbolDisplayFormat AliasQualifiedNameFormat =
        SymbolDisplayFormat.FullyQualifiedFormat
            .WithGenericsOptions(SymbolDisplayGenericsOptions.None)
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

    /// <summary>Gets the extern alias qualifying a symbol's assembly, or null when it is reachable via <c>global::</c>.</summary>
    /// <param name="symbol">The symbol whose containing assembly is inspected.</param>
    /// <param name="context">The generation context supplying the compilation and the pass-wide alias cache.</param>
    /// <returns>The extern alias, or null.</returns>
    internal static string? GetExternAlias(ISymbol symbol, in InterfaceGenerationContext context)
    {
        var assembly = symbol.ContainingAssembly;
        if (context.Compilation is not { } compilation || assembly is null)
        {
            return null;
        }

        // The assembly -> alias mapping is invariant for the whole pass, so a single metadata-reference lookup per
        // assembly is cached and reused across every type node of every interface (QualifyType runs very often).
        var cache = context.AssemblyAliasCache;
        if (cache.TryGetValue(assembly, out var cachedAlias))
        {
            return cachedAlias;
        }

        var resolved = ResolveExternAlias(assembly, compilation);
        cache[assembly] = resolved;
        return resolved;
    }

    /// <summary>Resolves the extern alias for an assembly by inspecting its metadata reference.</summary>
    /// <param name="assembly">The assembly to resolve.</param>
    /// <param name="compilation">The compilation supplying the assembly's metadata reference.</param>
    /// <returns>The extern alias, or null when the assembly is reachable via <c>global::</c>.</returns>
    internal static string? ResolveExternAlias(IAssemblySymbol assembly, CSharpCompilation compilation)
    {
        var aliases = compilation.GetMetadataReference(assembly)?.Properties.Aliases ?? default;
        if (aliases.IsDefaultOrEmpty)
        {
            return null;
        }

        // A reference that is also aliased "global" stays reachable without qualification.
        foreach (var alias in aliases)
        {
            if (alias == "global")
            {
                return null;
            }
        }

        return aliases[0];
    }

    /// <summary>Determines whether a type (or any array element or type argument) lives behind an extern alias.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="context">The generation context supplying the compilation and the pass-wide alias cache.</param>
    /// <returns><see langword="true"/> when the type involves an extern-aliased assembly.</returns>
    internal static bool ContainsAliasedType(ITypeSymbol type, in InterfaceGenerationContext context)
    {
        if (type is IArrayTypeSymbol array)
        {
            return ContainsAliasedType(array.ElementType, context);
        }

        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        if (GetExternAlias(named, context) is not null)
        {
            return true;
        }

        foreach (var argument in named.TypeArguments)
        {
            if (ContainsAliasedType(argument, context))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Fully qualifies a type, using <c>alias::</c> for any extern-aliased assembly and recording the alias.</summary>
    /// <param name="type">The type to qualify.</param>
    /// <param name="context">The generation context, whose extern-alias collector records the aliases used.</param>
    /// <returns>The fully-qualified type name.</returns>
    internal static string QualifyType(ITypeSymbol type, in InterfaceGenerationContext context)
    {
        // The common case is no aliased type at all: Roslyn's own fully-qualified rendering is exactly right. That
        // rendering depends only on the type symbol, so it is memoized per pass and reused across every occurrence.
        // The aliased path is never cached: it records the aliases it uses into the per-interface collector.
        if (ContainsAliasedType(type, context))
        {
            return AliasedDisplay(type, context);
        }

        var cache = context.QualifiedTypeCache;
        if (cache.TryGetValue(type, out var cached))
        {
            return cached;
        }

        var qualified = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        cache[type] = qualified;
        return qualified;
    }

    /// <summary>Renders a type's fully-qualified name with <c>alias::</c> for extern-aliased assemblies, recursively.</summary>
    /// <param name="type">The type to render.</param>
    /// <param name="context">The generation context, whose extern-alias collector records the aliases used.</param>
    /// <returns>The rendered type name.</returns>
    internal static string AliasedDisplay(ITypeSymbol type, in InterfaceGenerationContext context) =>
        type switch
        {
            IArrayTypeSymbol array => $"{AliasedDisplay(array.ElementType, context)}[]",
            INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable =>
                $"{AliasedDisplay(nullable.TypeArguments[0], context)}?",
            INamedTypeSymbol named => AliasedNamedDisplay(named, context),
            _ => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        };

    /// <summary>Renders a named type's alias-qualified name with recursively-rendered type arguments.</summary>
    /// <param name="named">The named type to render.</param>
    /// <param name="context">The generation context, whose extern-alias collector records the aliases used.</param>
    /// <returns>The rendered type name.</returns>
    internal static string AliasedNamedDisplay(INamedTypeSymbol named, in InterfaceGenerationContext context)
    {
        var alias = GetExternAlias(named, context);
        if (alias is not null)
        {
            _ = context.ExternAliases.Add(alias);
        }

        var effectiveAlias = alias ?? "global";
        var name = $"{effectiveAlias}::{named.ToDisplayString(AliasQualifiedNameFormat)}";
        if (named.TypeArguments.IsEmpty)
        {
            return name;
        }

        var arguments = new string[named.TypeArguments.Length];
        for (var i = 0; i < named.TypeArguments.Length; i++)
        {
            arguments[i] = AliasedDisplay(named.TypeArguments[i], context);
        }

        return $"{name}<{string.Join(", ", arguments)}>";
    }
}
