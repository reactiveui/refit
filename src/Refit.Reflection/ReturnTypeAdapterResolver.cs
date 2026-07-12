// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>Matches a Refit method's return type against the adapters registered for the reflection request builder.</summary>
/// <remarks>
/// Matching reads only type metadata, so it stays trim-safe and Native-AOT-safe and can run while building method
/// metadata; closing a generic adapter over the return type needs runtime generic instantiation and is deferred to
/// <see cref="ResolveClosedAdapterType"/> at delegate-build time, which the reflection builder already gates behind
/// <c>[RequiresDynamicCode]</c>.
/// </remarks>
internal static class ReturnTypeAdapterResolver
{
    /// <summary>Resolves the result type wrapped by a registered adapter that surfaces <paramref name="returnType"/>.</summary>
    /// <param name="returnType">The declared return type of the interface method.</param>
    /// <param name="registeredAdapters">The adapter types registered on the settings.</param>
    /// <param name="resultType">The result type the HTTP call materializes (the adapter's <c>TResult</c>) when matched.</param>
    /// <returns><see langword="true"/> when a registered adapter surfaces the return type; otherwise <see langword="false"/>.</returns>
    [RequiresUnreferencedCode("Resolving return-type adapters inspects adapter interface metadata that trimming may remove.")]
    internal static bool TryResolveResultType(
        Type returnType,
        IList<Type> registeredAdapters,
        [NotNullWhen(true)] out Type? resultType)
    {
        for (var i = 0; i < registeredAdapters.Count; i++)
        {
            if (TryMatch(returnType, registeredAdapters[i], out _, out resultType))
            {
                return true;
            }
        }

        resultType = null;
        return false;
    }

    /// <summary>Resolves the closed adapter type to instantiate for <paramref name="returnType"/>.</summary>
    /// <param name="returnType">The declared return type of the interface method.</param>
    /// <param name="registeredAdapters">The adapter types registered on the settings.</param>
    /// <returns>The closed <see cref="IReturnTypeAdapter{TReturn, TResult}"/> type, or <see langword="null"/> when none matches.</returns>
    [RequiresUnreferencedCode("Resolving return-type adapters inspects adapter interface metadata that trimming may remove.")]
    [RequiresDynamicCode("Closing a generic return-type adapter over the return type requires runtime generic type instantiation.")]
    internal static Type? ResolveClosedAdapterType(Type returnType, IList<Type> registeredAdapters)
    {
        for (var i = 0; i < registeredAdapters.Count; i++)
        {
            var adapter = registeredAdapters[i];
            if (TryMatch(returnType, adapter, out var typeArguments, out _))
            {
                return typeArguments.Length == 0 ? adapter : adapter.MakeGenericType(typeArguments);
            }
        }

        return null;
    }

    /// <summary>Matches an adapter type against a return type using only metadata, without instantiating anything.</summary>
    /// <param name="returnType">The declared return type the adapter must surface.</param>
    /// <param name="adapter">The registered adapter type, either closed or an open generic definition.</param>
    /// <param name="typeArguments">The type arguments needed to close a generic adapter, or empty for a closed one.</param>
    /// <param name="resultType">The result type the adapter wraps (its <c>TResult</c>) when matched.</param>
    /// <returns><see langword="true"/> when the adapter surfaces the return type; otherwise <see langword="false"/>.</returns>
    [RequiresUnreferencedCode("Resolving return-type adapters inspects adapter interface metadata that trimming may remove.")]
    private static bool TryMatch(
        Type returnType,
        Type adapter,
        out Type[] typeArguments,
        [NotNullWhen(true)] out Type? resultType)
    {
        typeArguments = [];
        resultType = null;

        if (adapter is null)
        {
            return false;
        }

        return adapter.IsGenericTypeDefinition
            ? TryMatchGenericDefinition(returnType, adapter, out typeArguments, out resultType)
            : TryMatchClosed(returnType, adapter, out resultType);
    }

    /// <summary>Matches a non-generic (or already closed) adapter whose <c>TReturn</c> equals the return type.</summary>
    /// <param name="returnType">The declared return type the adapter must surface.</param>
    /// <param name="adapter">The closed adapter type.</param>
    /// <param name="resultType">The adapter's <c>TResult</c> when matched.</param>
    /// <returns><see langword="true"/> when the adapter surfaces the return type; otherwise <see langword="false"/>.</returns>
    [RequiresUnreferencedCode("Resolving return-type adapters inspects adapter interface metadata that trimming may remove.")]
    private static bool TryMatchClosed(Type returnType, Type adapter, [NotNullWhen(true)] out Type? resultType)
    {
        foreach (var implemented in adapter.GetInterfaces())
        {
            if (IsAdapterInterface(implemented) && implemented.GetGenericArguments()[0] == returnType)
            {
                resultType = implemented.GetGenericArguments()[1];
                return true;
            }
        }

        resultType = null;
        return false;
    }

    /// <summary>Matches an open generic adapter definition using the single-wrapper heuristic.</summary>
    /// <param name="returnType">The declared return type the adapter must surface.</param>
    /// <param name="adapter">The open generic adapter definition.</param>
    /// <param name="typeArguments">The return type's type arguments used to close the adapter when matched.</param>
    /// <param name="resultType">The adapter's <c>TResult</c> resolved against the return type when matched.</param>
    /// <returns><see langword="true"/> when the adapter surfaces the return type; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// The adapter's <c>TReturn</c> must be a constructed type whose type arguments are the adapter's type parameters
    /// in order (for example <c>Adapter&lt;T&gt; : IReturnTypeAdapter&lt;Wrapper&lt;T&gt;, T&gt;</c>), and whose
    /// generic definition matches the return type's, so <c>Wrapper&lt;X&gt;</c> closes the adapter over <c>X</c>.
    /// </remarks>
    [RequiresUnreferencedCode("Resolving return-type adapters inspects adapter interface metadata that trimming may remove.")]
    private static bool TryMatchGenericDefinition(
        Type returnType,
        Type adapter,
        out Type[] typeArguments,
        [NotNullWhen(true)] out Type? resultType)
    {
        typeArguments = [];
        resultType = null;

        if (!returnType.IsGenericType)
        {
            return false;
        }

        var returnArguments = returnType.GetGenericArguments();
        if (returnArguments.Length != adapter.GetGenericArguments().Length)
        {
            return false;
        }

        foreach (var implemented in adapter.GetInterfaces())
        {
            if (!IsAdapterInterface(implemented))
            {
                continue;
            }

            var templateReturn = implemented.GetGenericArguments()[0];
            if (!IsPositionalTypeParameters(templateReturn, returnType))
            {
                continue;
            }

            var resolved = ResolveTemplateResult(implemented.GetGenericArguments()[1], returnArguments);
            if (resolved is null)
            {
                continue;
            }

            typeArguments = returnArguments;
            resultType = resolved;
            return true;
        }

        return false;
    }

    /// <summary>Determines whether an implemented interface is a closed <see cref="IReturnTypeAdapter{TReturn, TResult}"/>.</summary>
    /// <param name="implemented">The implemented interface to test.</param>
    /// <returns><see langword="true"/> when the interface is a return-type adapter.</returns>
    private static bool IsAdapterInterface(Type implemented) =>
        implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IReturnTypeAdapter<,>);

    /// <summary>Determines whether the adapter's template <c>TReturn</c> is the return type's generic definition
    /// closed over the adapter's type parameters in declaration order.</summary>
    /// <param name="templateReturn">The adapter's declared <c>TReturn</c> (open constructed).</param>
    /// <param name="returnType">The declared return type of the interface method.</param>
    /// <returns><see langword="true"/> when the shapes line up positionally.</returns>
    private static bool IsPositionalTypeParameters(Type templateReturn, Type returnType)
    {
        if (!templateReturn.IsGenericType
            || templateReturn.GetGenericTypeDefinition() != returnType.GetGenericTypeDefinition())
        {
            return false;
        }

        var templateArguments = templateReturn.GetGenericArguments();
        for (var i = 0; i < templateArguments.Length; i++)
        {
            if (!templateArguments[i].IsGenericParameter || templateArguments[i].GenericParameterPosition != i)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Resolves the adapter's template <c>TResult</c> against the return type's type arguments.</summary>
    /// <param name="templateResult">The adapter's declared <c>TResult</c>.</param>
    /// <param name="returnArguments">The return type's type arguments, ordered by the adapter's type parameters.</param>
    /// <returns>The concrete result type, or <see langword="null"/> when it needs runtime generic instantiation.</returns>
    private static Type? ResolveTemplateResult(Type templateResult, Type[] returnArguments) =>
        templateResult switch
        {
            { IsGenericParameter: true } => returnArguments[templateResult.GenericParameterPosition],

            // A concrete, fully-closed TResult (for example a bare class) is used verbatim; a TResult that is itself
            // constructed over the adapter's type parameters would need MakeGenericType and is left to the caller.
            { ContainsGenericParameters: false } => templateResult,
            _ => null
        };
}
