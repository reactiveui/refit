// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;

namespace Refit;

/// <summary>Caches the result functions produced by an inner <see cref="IRequestBuilder"/> so each method is analyzed only once.</summary>
internal class CachedRequestBuilderImplementation : IRequestBuilder
{
    /// <summary>The request builder whose results are cached.</summary>
    private readonly IRequestBuilder _innerBuilder;

    /// <summary>Initializes a new instance of the <see cref="CachedRequestBuilderImplementation"/> class.</summary>
    /// <param name="innerBuilder">The request builder whose results are cached.</param>
    public CachedRequestBuilderImplementation(IRequestBuilder innerBuilder)
    {
        ArgumentExceptionHelper.ThrowIfNull(innerBuilder);
        _innerBuilder = innerBuilder;
    }

    /// <inheritdoc/>
    public RefitSettings Settings => _innerBuilder.Settings;

    /// <summary>Gets the cache of method keys to their built result functions.</summary>
    internal ConcurrentDictionary<
        MethodTableKey,
        Func<HttpClient, object[], object?>
    > MethodDictionary { get; } = new();

    /// <inheritdoc/>
    [RequiresUnreferencedCode("Building request delegates from reflected method metadata requires generic method metadata to be available at runtime.")]
    [RequiresDynamicCode("Building request delegates from reflected method metadata requires runtime generic method instantiation.")]
    public Func<HttpClient, object[], object?> BuildRestResultFuncForMethod(
        string methodName,
        Type[]? parameterTypes = null,
        Type[]? genericArgumentTypes = null)
    {
        var cacheKey = new MethodTableKey(
            methodName,
            parameterTypes ?? [],
            genericArgumentTypes ?? []);

        if (MethodDictionary.TryGetValue(cacheKey, out var methodFunc))
        {
            return methodFunc;
        }

        // use GetOrAdd with cloned array method table key. This prevents the array from being modified, breaking the dictionary.
        return MethodDictionary.GetOrAdd(
            new(
                methodName,
                parameterTypes is not null ? (Type[])parameterTypes.Clone() : [],
                genericArgumentTypes is not null ? (Type[])genericArgumentTypes.Clone() : []),
            _ =>
                _innerBuilder.BuildRestResultFuncForMethod(
                    methodName,
                    parameterTypes,
                    genericArgumentTypes));
    }
}
