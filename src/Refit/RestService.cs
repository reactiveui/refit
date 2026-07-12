// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>Creates Refit interface implementations.</summary>
public static class RestService
{
    /// <summary>Caches the resolved generated implementation type per interface.</summary>
    private static readonly ConcurrentDictionary<Type, Type> _typeMapping = new();

    /// <summary>Holds registered source-generated implementation factories per interface.</summary>
    private static readonly ConcurrentDictionary<Type, Func<HttpClient, IRequestBuilder, object>> _generatedFactories =
        new();

    /// <summary>Holds source-generated factories that only need settings and avoid request-builder reflection.</summary>
    private static readonly ConcurrentDictionary<Type, Func<HttpClient, RefitSettings, object>> _generatedSettingsFactories =
        new();

    /// <summary>Registers a source-generated Refit implementation factory.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <param name="factory">The generated implementation factory.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterGeneratedFactory(
        Type refitInterfaceType,
        Func<HttpClient, IRequestBuilder, object> factory)
    {
        ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

        ArgumentExceptionHelper.ThrowIfNull(factory);

        _generatedFactories[refitInterfaceType] = factory;
    }

    /// <summary>Registers a source-generated Refit implementation factory.</summary>
    /// <typeparam name="T">The Refit interface type.</typeparam>
    /// <param name="factory">The generated implementation factory.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterGeneratedFactory<T>(Func<HttpClient, IRequestBuilder, T> factory)
    {
        ArgumentExceptionHelper.ThrowIfNull(factory);

        GeneratedFactory<T>.Factory = factory;
        _generatedFactories[typeof(T)] = (client, requestBuilder) => factory(client, requestBuilder)!;
    }

    /// <summary>Registers a source-generated Refit implementation factory that does not need the reflection request builder.</summary>
    /// <typeparam name="T">The Refit interface type.</typeparam>
    /// <param name="factory">The generated implementation factory.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterGeneratedSettingsFactory<T>(Func<HttpClient, RefitSettings, T> factory)
    {
        ArgumentExceptionHelper.ThrowIfNull(factory);

        GeneratedSettingsFactory<T>.Factory = factory;
        _generatedSettingsFactories[typeof(T)] = (client, settings) => factory(client, settings)!;
    }

    /// <summary>Create a source-generated Refit implementation without falling back to reflection.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <typeparamref name="T"/>.</exception>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public static T ForGenerated<T>(HttpClient client) => ForGenerated<T>(client, new());

    /// <summary>Create a source-generated Refit implementation without falling back to reflection.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the generated client.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <typeparamref name="T"/>.</exception>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public static T ForGenerated<T>(HttpClient client, RefitSettings settings)
    {
        ArgumentExceptionHelper.ThrowIfNull(client);
        ArgumentExceptionHelper.ThrowIfNull(settings);

        if (GeneratedSettingsFactory<T>.Factory is { } settingsFactory)
        {
            return settingsFactory(client, settings);
        }

        if (_generatedSettingsFactories.TryGetValue(typeof(T), out var untypedSettingsFactory))
        {
            return (T)untypedSettingsFactory(client, settings);
        }

        if (_generatedFactories.TryGetValue(typeof(T), out var untypedFactory))
        {
            return (T)untypedFactory(client, new GeneratedOnlyRequestBuilder(settings));
        }

        if (GeneratedFactory<T>.Factory is { } factory)
        {
            return factory(client, new GeneratedOnlyRequestBuilder(settings));
        }

        throw CreateMissingGeneratedFactoryException(typeof(T));
    }

    /// <summary>Create a source-generated Refit implementation without falling back to reflection.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="hostUrl">Base address the implementation will use.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <typeparamref name="T"/>.</exception>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public static T ForGenerated<T>(string hostUrl) => ForGenerated<T>(hostUrl, new());

    /// <summary>Create a source-generated Refit implementation without falling back to reflection.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="hostUrl">Base address the implementation will use.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the generated client.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <typeparamref name="T"/>.</exception>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public static T ForGenerated<T>(string hostUrl, RefitSettings settings)
    {
        ArgumentExceptionHelper.ThrowIfNull(settings);

        var client = CreateHttpClient(hostUrl, settings);
        return ForGenerated<T>(client, settings);
    }

    /// <summary>Create a source-generated Refit implementation without falling back to reflection.</summary>
    /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the generated client.</param>
    /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <paramref name="refitInterfaceType"/>.</exception>
    public static object ForGenerated(
        Type refitInterfaceType,
        HttpClient client,
        RefitSettings settings)
    {
        ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);
        ArgumentExceptionHelper.ThrowIfNull(client);
        ArgumentExceptionHelper.ThrowIfNull(settings);

        if (_generatedSettingsFactories.TryGetValue(refitInterfaceType, out var settingsFactory))
        {
            return settingsFactory(client, settings);
        }

        if (_generatedFactories.TryGetValue(refitInterfaceType, out var factory))
        {
            return factory(client, new GeneratedOnlyRequestBuilder(settings));
        }

        throw CreateMissingGeneratedFactoryException(refitInterfaceType);
    }

    /// <summary>Create a source-generated Refit implementation without falling back to reflection.</summary>
    /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
    /// <param name="hostUrl">Base address the implementation will use.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the generated client.</param>
    /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <paramref name="refitInterfaceType"/>.</exception>
    public static object ForGenerated(
        Type refitInterfaceType,
        string hostUrl,
        RefitSettings settings)
    {
        ArgumentExceptionHelper.ThrowIfNull(settings);

        var client = CreateHttpClient(hostUrl, settings);
        return ForGenerated(refitInterfaceType, client, settings);
    }

    /// <summary>Generate a Refit implementation of the specified interface.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="builder"><see cref="IRequestBuilder"/> to use to build requests.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    [RequiresUnreferencedCode("Creating a generated client through the reflection path requires runtime type lookup and constructor metadata.")]
    public static T For<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        T>(HttpClient client, IRequestBuilder<T> builder) => (T)For(typeof(T), client, builder);

    /// <summary>Generate a Refit implementation of the specified interface.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresUnreferencedCode("Creating a generated client through the reflection path requires runtime type lookup and request metadata.")]
    public static T For<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        T>(HttpClient client, RefitSettings? settings)
    {
        // A generated settings factory means every method builds its request inline, so the reflection
        // request builder (and the Refit.Reflection assembly) is never needed for this interface.
        if (GeneratedSettingsFactory<T>.Factory is { } settingsFactory)
        {
            return settingsFactory(client, settings ?? new());
        }

        if (_generatedSettingsFactories.TryGetValue(typeof(T), out var untypedSettingsFactory))
        {
            return (T)untypedSettingsFactory(client, settings ?? new());
        }

        var requestBuilder = RequestBuilder.ForType<T>(settings);

        return For(client, requestBuilder);
    }

    /// <summary>Generate a Refit implementation of the specified interface.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresUnreferencedCode("Creating a generated client through the reflection path requires runtime type lookup and request metadata.")]
    public static T For<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        T>(HttpClient client) => For<T>(client, (RefitSettings?)null);

    /// <summary>Generate a Refit implementation of the specified interface.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="hostUrl">Base address the implementation will use.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresUnreferencedCode("Creating a generated client through the reflection path requires runtime type lookup and request metadata.")]
    public static T For<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        T>(string hostUrl, RefitSettings? settings)
    {
        var client = CreateHttpClient(hostUrl, settings);

        return For<T>(client, settings);
    }

    /// <summary>Generate a Refit implementation of the specified interface.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="hostUrl">Base address the implementation will use.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresUnreferencedCode("Creating a generated client through the reflection path requires runtime type lookup and request metadata.")]
    public static T For<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        T>(string hostUrl) => For<T>(hostUrl, null);

    /// <summary>Generate a Refit implementation of the specified interface.</summary>
    /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="builder"><see cref="IRequestBuilder"/> to use to build requests.</param>
    /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
    [RequiresUnreferencedCode("Creating a generated client by Type requires runtime type lookup and constructor metadata.")]
    public static object For(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type refitInterfaceType,
        HttpClient client,
        IRequestBuilder builder)
    {
        if (_generatedFactories.TryGetValue(refitInterfaceType, out var factory))
        {
            return factory(client, builder);
        }

        var generatedType = _typeMapping.GetOrAdd(refitInterfaceType, GetGeneratedType);

        return Activator.CreateInstance(generatedType, client, builder)!;
    }

    /// <summary>Generate a Refit implementation of the specified interface.</summary>
    /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
    /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
    [RequiresUnreferencedCode("Creating a generated client through the reflection path requires runtime type lookup and request metadata.")]
    public static object For(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type refitInterfaceType,
        HttpClient client,
        RefitSettings? settings)
    {
        // A generated settings factory means every method builds its request inline, so the reflection
        // request builder (and the Refit.Reflection assembly) is never needed for this interface.
        if (_generatedSettingsFactories.TryGetValue(refitInterfaceType, out var settingsFactory))
        {
            return settingsFactory(client, settings ?? new());
        }

        var requestBuilder = RequestBuilder.ForType(refitInterfaceType, settings);

        return For(refitInterfaceType, client, requestBuilder);
    }

    /// <summary>Generate a Refit implementation of the specified interface.</summary>
    /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
    [RequiresUnreferencedCode("Creating a generated client through the reflection path requires runtime type lookup and request metadata.")]
    public static object For(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type refitInterfaceType,
        HttpClient client) => For(refitInterfaceType, client, (RefitSettings?)null);

    /// <summary>Generate a Refit implementation of the specified interface.</summary>
    /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
    /// <param name="hostUrl">Base address the implementation will use.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
    /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
    [RequiresUnreferencedCode("Creating a generated client through the reflection path requires runtime type lookup and request metadata.")]
    public static object For(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type refitInterfaceType,
        string hostUrl,
        RefitSettings? settings)
    {
        var client = CreateHttpClient(hostUrl, settings);

        return For(refitInterfaceType, client, settings);
    }

    /// <summary>Generate a Refit implementation of the specified interface.</summary>
    /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
    /// <param name="hostUrl">Base address the implementation will use.</param>
    /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
    [RequiresUnreferencedCode("Creating a generated client through the reflection path requires runtime type lookup and request metadata.")]
    public static object For(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type refitInterfaceType,
        string hostUrl) => For(refitInterfaceType, hostUrl, null);

    /// <summary>Create an <see cref="HttpClient"/> with <paramref name="hostUrl"/> as the base address.</summary>
    /// <param name="hostUrl">Base address.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
    /// <returns>A <see cref="HttpClient"/> with the various parameters provided.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="hostUrl"/> is null or whitespace.</exception>
    public static HttpClient CreateHttpClient(string hostUrl, RefitSettings? settings)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(hostUrl);
#else
        if (string.IsNullOrWhiteSpace(hostUrl))
        {
            throw new ArgumentException(
                $"`{nameof(hostUrl)}` must not be null or whitespace.",
                nameof(hostUrl));
        }
#endif

        // check to see if user provided custom auth token
        HttpMessageHandler? innerHandler = null;
        if (settings is not null)
        {
            if (settings.HttpMessageHandlerFactory is not null)
            {
                innerHandler = settings.HttpMessageHandlerFactory();
            }

            if (settings.AuthorizationHeaderValueGetter is not null)
            {
                innerHandler = new AuthenticatedHttpClientHandler(
                    settings.AuthorizationHeaderValueGetter,
                    innerHandler);
            }
        }

        // Under RFC 3986 resolution the trailing slash is significant (it controls whether a relative path is
        // appended to or replaces the base path), so preserve the host URL as supplied. The legacy mode trims it
        // because it prepends the base path itself.
        var baseAddress = settings?.UrlResolution == UrlResolutionMode.Rfc3986 ? hostUrl : hostUrl.TrimEnd('/');
        return new(innerHandler ?? new HttpClientHandler()) { BaseAddress = new(baseAddress) };
    }

    /// <summary>Resolves the generated implementation type for a Refit interface.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <returns>The generated implementation type.</returns>
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    [RequiresUnreferencedCode("Resolving a generated client type by name requires runtime type lookup.")]
    private static Type GetGeneratedType(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type refitInterfaceType)
    {
        var typeName = UniqueName.ForType(refitInterfaceType);

        return Type.GetType(typeName, false)
            ?? throw CreateMissingGeneratedFactoryException(refitInterfaceType);
    }

    /// <summary>Creates the exception thrown when no source-generated implementation is available.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <returns>The generated-client exception.</returns>
    private static InvalidOperationException CreateMissingGeneratedFactoryException(Type refitInterfaceType)
    {
        var message =
            refitInterfaceType.Name
            + " doesn't look like a Refit interface. Make sure it has at least one "
            + "method with a Refit HTTP method attribute, the Refit source generator is installed in the project, "
            + "and your build produced the generated client. For Native AOT or trimmed apps, prefer generated clients "
            + "plus source-generated System.Text.Json metadata.";

        return new(message);
    }

    /// <summary>Holds the typed generated factory for a single Refit interface.</summary>
    /// <typeparam name="T">The Refit interface type.</typeparam>
    internal static class GeneratedFactory<T>
    {
        /// <summary>Gets or sets the generated implementation factory.</summary>
        internal static Func<HttpClient, IRequestBuilder, T>? Factory { get; set; }
    }

    /// <summary>Holds the typed generated settings factory for a single Refit interface.</summary>
    /// <typeparam name="T">The Refit interface type.</typeparam>
    internal static class GeneratedSettingsFactory<T>
    {
        /// <summary>Gets or sets the generated implementation factory.</summary>
        internal static Func<HttpClient, RefitSettings, T>? Factory { get; set; }
    }
}
