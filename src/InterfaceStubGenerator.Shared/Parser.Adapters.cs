// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>Discovery and matching for <c>IReturnTypeAdapter</c> implementations, so generated methods surface custom
/// return types (for example <c>IObservable&lt;T&gt;</c> or <c>Result&lt;T&gt;</c>) with a direct <c>Adapt</c> call.</content>
internal static partial class Parser
{
    /// <summary>The metadata name of the <c>Refit.IReturnTypeAdapter`2</c> interface.</summary>
    private const string ReturnTypeAdapterMetadataName = "Refit.IReturnTypeAdapter`2";

    /// <summary>Resolves the <c>Refit.IReturnTypeAdapter`2</c> interface symbol, or null when Refit is unavailable.</summary>
    /// <param name="compilation">The compilation to resolve against.</param>
    /// <returns>The interface symbol, or <see langword="null"/>.</returns>
    internal static INamedTypeSymbol? ResolveReturnTypeAdapterInterface(Compilation compilation) =>
        compilation.GetTypeByMetadataName(ReturnTypeAdapterMetadataName);

    /// <summary>Discovers the source-declared types that implement <c>IReturnTypeAdapter</c> in the compilation.</summary>
    /// <param name="compilation">The compilation whose source assembly is scanned.</param>
    /// <param name="adapterInterface">The resolved <c>IReturnTypeAdapter`2</c> symbol, or null.</param>
    /// <param name="cancellationToken">A token to observe while scanning.</param>
    /// <returns>The adapter type definitions, or an empty array when none exist.</returns>
    internal static INamedTypeSymbol[] DiscoverReturnTypeAdapters(
        Compilation compilation,
        INamedTypeSymbol? adapterInterface,
        CancellationToken cancellationToken)
    {
        if (adapterInterface is null)
        {
            return [];
        }

        var adapters = new List<INamedTypeSymbol>();
        CollectReturnTypeAdapters(compilation.Assembly.GlobalNamespace, adapterInterface, adapters, cancellationToken);
        return [.. adapters];
    }

    /// <summary>Recursively collects types implementing <c>IReturnTypeAdapter</c> from a namespace.</summary>
    /// <param name="namespaceSymbol">The namespace to scan.</param>
    /// <param name="adapterInterface">The resolved <c>IReturnTypeAdapter`2</c> symbol.</param>
    /// <param name="adapters">The accumulator for discovered adapter types.</param>
    /// <param name="cancellationToken">A token to observe while scanning.</param>
    internal static void CollectReturnTypeAdapters(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol adapterInterface,
        List<INamedTypeSymbol> adapters,
        CancellationToken cancellationToken)
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // A namespace's members are either nested namespaces or named types; anything that is not a nested
            // namespace is a named type.
            if (member is INamespaceSymbol nestedNamespace)
            {
                CollectReturnTypeAdapters(nestedNamespace, adapterInterface, adapters, cancellationToken);
            }
            else
            {
                CollectReturnTypeAdapterType((INamedTypeSymbol)member, adapterInterface, adapters);
            }
        }
    }

    /// <summary>Adds a type and its nested types to the adapter list when they implement <c>IReturnTypeAdapter</c>.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="adapterInterface">The resolved <c>IReturnTypeAdapter`2</c> symbol.</param>
    /// <param name="adapters">The accumulator for discovered adapter types.</param>
    internal static void CollectReturnTypeAdapterType(
        INamedTypeSymbol type,
        INamedTypeSymbol adapterInterface,
        List<INamedTypeSymbol> adapters)
    {
        if (type.TypeKind is TypeKind.Class or TypeKind.Struct
            && !type.IsAbstract
            && FindImplementedAdapterInterface(type, adapterInterface) is not null)
        {
            adapters.Add(type);
        }

        foreach (var nested in type.GetTypeMembers())
        {
            CollectReturnTypeAdapterType(nested, adapterInterface, adapters);
        }
    }

    /// <summary>Finds a registered adapter that surfaces the method's return type and its wrapped result type.</summary>
    /// <param name="returnType">The declared return type of the interface method.</param>
    /// <param name="context">The generation context carrying the discovered adapters.</param>
    /// <param name="closedAdapter">The closed adapter type to instantiate when matched.</param>
    /// <param name="resultType">The result type the HTTP call materializes when matched.</param>
    /// <returns><see langword="true"/> when a discovered adapter surfaces the return type; otherwise <see langword="false"/>.</returns>
    internal static bool TryMatchReturnTypeAdapter(
        ITypeSymbol returnType,
        InterfaceGenerationContext context,
        out INamedTypeSymbol closedAdapter,
        out ITypeSymbol resultType)
    {
        closedAdapter = null!;
        resultType = null!;

        var adapterInterface = context.ReturnTypeAdapterInterface;
        if (adapterInterface is null
            || context.ReturnTypeAdapters.Length == 0
            || returnType is not INamedTypeSymbol namedReturn)
        {
            return false;
        }

        foreach (var adapter in context.ReturnTypeAdapters)
        {
            var closed = CloseAdapterForReturn(adapter, namedReturn, adapterInterface);
            if (closed is null)
            {
                continue;
            }

            var implemented = FindConstructedAdapterInterface(closed, namedReturn, adapterInterface);
            if (implemented is not null)
            {
                closedAdapter = closed;
                resultType = implemented.TypeArguments[1];
                return true;
            }
        }

        return false;
    }

    /// <summary>Closes an adapter definition over the return type's type arguments, or returns it when non-generic.</summary>
    /// <param name="adapter">The adapter type definition.</param>
    /// <param name="returnType">The declared return type supplying the type arguments.</param>
    /// <param name="adapterInterface">The resolved <c>IReturnTypeAdapter`2</c> symbol.</param>
    /// <returns>The closed adapter type, or <see langword="null"/> when it cannot surface the return type.</returns>
    internal static INamedTypeSymbol? CloseAdapterForReturn(
        INamedTypeSymbol adapter,
        INamedTypeSymbol returnType,
        INamedTypeSymbol adapterInterface)
    {
        if (adapter.TypeParameters.IsEmpty)
        {
            return adapter;
        }

        // Single-wrapper heuristic: the adapter's TReturn must be its type parameters wrapped in the return type's
        // generic definition, so a Wrapper<X> return closes Adapter<T> : IReturnTypeAdapter<Wrapper<T>, ...> over X.
        if (returnType.TypeArguments.Length != adapter.TypeParameters.Length)
        {
            return null;
        }

        // Every adapter reaching here was discovered by CollectReturnTypeAdapterType, which only keeps types that
        // implement the adapter interface, so this lookup always resolves.
        var openInterface = FindImplementedAdapterInterface(adapter, adapterInterface)!;
        return openInterface.TypeArguments[0] is INamedTypeSymbol templateReturn
            && SymbolEqualityComparer.Default.Equals(templateReturn.OriginalDefinition, returnType.OriginalDefinition)
            && TryMapAdapterTypeArguments(templateReturn, returnType, adapter, out var adapterOrdered)
            ? adapter.Construct(adapterOrdered)
            : null;
    }

    /// <summary>Maps a wrapper return type's arguments onto the adapter's type parameters, in adapter declaration order.</summary>
    /// <param name="templateReturn">The adapter's declared <c>TReturn</c> (open constructed over the adapter's parameters).</param>
    /// <param name="returnType">The declared return type supplying the concrete arguments.</param>
    /// <param name="adapter">The adapter type definition.</param>
    /// <param name="adapterOrdered">The concrete arguments ordered by the adapter's type parameters when the map succeeds.</param>
    /// <returns><see langword="true"/> when every adapter type parameter is bound consistently.</returns>
    /// <remarks>
    /// Supports reordered and concrete-mixed wrappers, not only the in-order case: each template argument that is one of
    /// the adapter's type parameters binds the return type's argument at that position, and each concrete template
    /// argument must equal the return type's argument. So <c>Adapter&lt;T1, T2&gt; : IReturnTypeAdapter&lt;Wrapper&lt;T2, T1&gt;, …&gt;</c>
    /// closes over a <c>Wrapper&lt;A, B&gt;</c> return as <c>Adapter&lt;B, A&gt;</c>. The sole caller invokes this only after the
    /// template return and the return type share a generic definition and arity, so the argument counts are equal.
    /// </remarks>
    internal static bool TryMapAdapterTypeArguments(
        INamedTypeSymbol templateReturn,
        INamedTypeSymbol returnType,
        INamedTypeSymbol adapter,
        out ITypeSymbol[] adapterOrdered)
    {
        adapterOrdered = [];
        var templateArguments = templateReturn.TypeArguments;
        var returnArguments = returnType.TypeArguments;
        var mapped = new ITypeSymbol?[adapter.TypeParameters.Length];
        for (var i = 0; i < templateArguments.Length; i++)
        {
            if (!TryBindAdapterArgument(templateArguments[i], returnArguments[i], adapter, mapped))
            {
                return false;
            }
        }

        foreach (var argument in mapped)
        {
            if (argument is null)
            {
                return false;
            }
        }

        adapterOrdered = mapped!;
        return true;
    }

    /// <summary>Binds one template argument to its return-type argument, either through a type parameter or an exact match.</summary>
    /// <param name="templateArgument">The adapter template's argument (a type parameter or a concrete type).</param>
    /// <param name="returnArgument">The return type's argument at the same position.</param>
    /// <param name="adapter">The adapter type definition owning the type parameters.</param>
    /// <param name="mapped">The per-type-parameter binding slots, filled by position.</param>
    /// <returns><see langword="true"/> when the argument binds consistently.</returns>
    internal static bool TryBindAdapterArgument(
        ITypeSymbol templateArgument,
        ITypeSymbol returnArgument,
        INamedTypeSymbol adapter,
        ITypeSymbol?[] mapped)
    {
        if (templateArgument is not ITypeParameterSymbol typeParameter
            || !SymbolEqualityComparer.Default.Equals(typeParameter.ContainingSymbol, adapter))
        {
            return SymbolEqualityComparer.Default.Equals(templateArgument, returnArgument);
        }

        var position = typeParameter.Ordinal;
        if (mapped[position] is { } existing)
        {
            return SymbolEqualityComparer.Default.Equals(existing, returnArgument);
        }

        mapped[position] = returnArgument;
        return true;
    }

    /// <summary>Gets the closed <c>IReturnTypeAdapter</c> interface whose <c>TReturn</c> equals the return type.</summary>
    /// <param name="closedAdapter">The closed adapter type.</param>
    /// <param name="returnType">The declared return type the adapter must surface.</param>
    /// <param name="adapterInterface">The resolved <c>IReturnTypeAdapter`2</c> symbol.</param>
    /// <returns>The matching constructed interface, or <see langword="null"/>.</returns>
    internal static INamedTypeSymbol? FindConstructedAdapterInterface(
        INamedTypeSymbol closedAdapter,
        INamedTypeSymbol returnType,
        INamedTypeSymbol adapterInterface)
    {
        foreach (var implemented in closedAdapter.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, adapterInterface)
                && SymbolEqualityComparer.Default.Equals(implemented.TypeArguments[0], returnType))
            {
                return implemented;
            }
        }

        return null;
    }

    /// <summary>Gets any <c>IReturnTypeAdapter</c> interface a type implements.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="adapterInterface">The resolved <c>IReturnTypeAdapter`2</c> symbol.</param>
    /// <returns>The implemented adapter interface, or <see langword="null"/>.</returns>
    internal static INamedTypeSymbol? FindImplementedAdapterInterface(
        INamedTypeSymbol type,
        INamedTypeSymbol adapterInterface)
    {
        foreach (var implemented in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, adapterInterface))
            {
                return implemented;
            }
        }

        return null;
    }
}
