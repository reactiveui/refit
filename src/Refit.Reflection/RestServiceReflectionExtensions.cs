// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>Adds the reflection-backed client creation methods to <see cref="RestService"/>.</summary>
public static class RestServiceReflectionExtensions
{
    /// <summary>Caches the resolved generated implementation type per interface.</summary>
    private static readonly ConcurrentDictionary<Type, Type> _typeMapping = new();

    /// <summary>Extension members of <see cref="RestService"/> that use reflection when a client cannot be generated inline.</summary>
    extension(RestService)
    {
        /// <summary>Generate a Refit implementation of the specified interface.</summary>
        /// <typeparam name="T">Interface to create the implementation for.</typeparam>
        /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
        /// <param name="builder"><see cref="IRequestBuilder"/> to use to build requests.</param>
        /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
        public static T For<T>(HttpClient client, IRequestBuilder<T> builder) => (T)RestService.For(typeof(T), client, builder);

        /// <summary>Generate a Refit implementation of the specified interface.</summary>
        /// <typeparam name="T">Interface to create the implementation for.</typeparam>
        /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
        /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
        /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        public static T For<T>(HttpClient client, RefitSettings? settings)
        {
            var resolvedSettings = settings ?? new();
            ReflectionSupport.EnsureRegistered();

            // A generated settings factory means every method builds its request inline, so the reflection
            // request builder is never needed for this interface.
            return RestService.TryResolveInlineClient<T>(client, resolvedSettings, RestService._generatedRegistrationRunner, out var instance)
                ? instance
                : RestService.For(client, RequestBuilder.ForType<T>(settings));
        }

        /// <summary>Generate a Refit implementation of the specified interface.</summary>
        /// <typeparam name="T">Interface to create the implementation for.</typeparam>
        /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
        /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        public static T For<T>(HttpClient client) => RestService.For<T>(client, (RefitSettings?)null);

        /// <summary>Generate a Refit implementation of the specified interface.</summary>
        /// <typeparam name="T">Interface to create the implementation for.</typeparam>
        /// <param name="hostUrl">Base address the implementation will use.</param>
        /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
        /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        public static T For<T>(string hostUrl, RefitSettings? settings) =>
            RestService.For<T>(RestService.CreateHttpClient(hostUrl, settings), settings);

        /// <summary>Generate a Refit implementation of the specified interface.</summary>
        /// <typeparam name="T">Interface to create the implementation for.</typeparam>
        /// <param name="hostUrl">Base address the implementation will use.</param>
        /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        public static T For<T>(string hostUrl) => RestService.For<T>(hostUrl, null);

        /// <summary>Generate a Refit implementation of the specified interface.</summary>
        /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
        /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
        /// <param name="builder"><see cref="IRequestBuilder"/> to use to build requests.</param>
        /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
        public static object For(Type refitInterfaceType, HttpClient client, IRequestBuilder builder)
        {
            if (RestService._generatedFactories.TryGetValue(refitInterfaceType, out var factory))
            {
                return factory(client, builder);
            }

            var generatedType = _typeMapping.GetOrAdd(refitInterfaceType, GeneratedTypeResolver.Resolve);

            return Activator.CreateInstance(generatedType, client, builder)!;
        }

        /// <summary>Generate a Refit implementation of the specified interface.</summary>
        /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
        /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
        /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
        /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
        public static object For(Type refitInterfaceType, HttpClient client, RefitSettings? settings)
        {
            var resolvedSettings = settings ?? new();
            ReflectionSupport.EnsureRegistered();

            // A generated settings factory means every method builds its request inline, so the reflection
            // request builder is never needed for this interface.
            return RestService.TryResolveInlineClient(refitInterfaceType, client, resolvedSettings, RestService._generatedRegistrationRunner, out var instance)
                ? instance
                : RestService.For(refitInterfaceType, client, RequestBuilder.ForType(refitInterfaceType, settings));
        }

        /// <summary>Generate a Refit implementation of the specified interface.</summary>
        /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
        /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
        /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object For(Type refitInterfaceType, HttpClient client) =>
            RestService.For(refitInterfaceType, client, (RefitSettings?)null);

        /// <summary>Generate a Refit implementation of the specified interface.</summary>
        /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
        /// <param name="hostUrl">Base address the implementation will use.</param>
        /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
        /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object For(Type refitInterfaceType, string hostUrl, RefitSettings? settings) =>
            RestService.For(refitInterfaceType, RestService.CreateHttpClient(hostUrl, settings), settings);

        /// <summary>Generate a Refit implementation of the specified interface.</summary>
        /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
        /// <param name="hostUrl">Base address the implementation will use.</param>
        /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object For(Type refitInterfaceType, string hostUrl) =>
            RestService.For(refitInterfaceType, hostUrl, null);
    }
}
