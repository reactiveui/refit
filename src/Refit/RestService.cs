// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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

    /// <summary>Caches the module-initializer runner so resolving a client allocates no delegate.</summary>
    private static readonly Action<Type> _generatedRegistrationRunner = RunGeneratedRegistrations;

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public static T ForGenerated<T>(HttpClient client) => ForGenerated<T>(client, new());

    /// <summary>Create a source-generated Refit implementation without falling back to reflection.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the generated client.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <typeparamref name="T"/>.</exception>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public static T ForGenerated<T>(HttpClient client, RefitSettings settings)
    {
        ArgumentExceptionHelper.ThrowIfNull(client);
        ArgumentExceptionHelper.ThrowIfNull(settings);

#if NET5_0_OR_GREATER
        if (TryResolveGeneratedClient<T>(client, settings, _generatedRegistrationRunner, out var instance))
        {
            return instance;
        }

        throw CreateMissingGeneratedFactoryException(typeof(T));
#else
        return TryResolveGeneratedClient<T>(client, settings, _generatedRegistrationRunner, out var instance)
            ? instance
            : (T)CreateByGeneratedTypeName(typeof(T), client, settings);
#endif
    }

    /// <summary>Create a source-generated Refit implementation without falling back to reflection.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="hostUrl">Base address the implementation will use.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <typeparamref name="T"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public static T ForGenerated<T>(string hostUrl) => ForGenerated<T>(hostUrl, new());

    /// <summary>Create a source-generated Refit implementation without falling back to reflection.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="hostUrl">Base address the implementation will use.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the generated client.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <typeparamref name="T"/>.</exception>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
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

#if NET5_0_OR_GREATER
        if (TryResolveGeneratedClient(refitInterfaceType, client, settings, _generatedRegistrationRunner, out var instance))
        {
            return instance;
        }

        throw CreateMissingGeneratedFactoryException(refitInterfaceType);
#else
        return TryResolveGeneratedClient(refitInterfaceType, client, settings, _generatedRegistrationRunner, out var instance)
            ? instance
            : CreateByGeneratedTypeName(refitInterfaceType, client, settings);
#endif
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
            DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        T>(HttpClient client, IRequestBuilder<T> builder) => (T)For(typeof(T), client, builder);

    /// <summary>Generate a Refit implementation of the specified interface.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresUnreferencedCode("Creating a generated client through the reflection path requires runtime type lookup and request metadata.")]
    public static T For<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        T>(HttpClient client, RefitSettings? settings)
    {
        var resolvedSettings = settings ?? new();

        // A generated settings factory means every method builds its request inline, so the reflection
        // request builder (and the Refit.Reflection assembly) is never needed for this interface.
        if (TryResolveInlineClient<T>(client, resolvedSettings, _generatedRegistrationRunner, out var instance))
        {
            return instance;
        }

        var requestBuilder = RequestBuilder.ForType<T>(settings);

        return For(client, requestBuilder);
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
    [RequiresUnreferencedCode("Creating a generated client through the reflection path requires runtime type lookup and request metadata.")]
    public static T For<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        T>(HttpClient client) => For<T>(client, (RefitSettings?)null);

    /// <summary>Generate a Refit implementation of the specified interface.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="hostUrl">Base address the implementation will use.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresUnreferencedCode("Creating a generated client through the reflection path requires runtime type lookup and request metadata.")]
    public static T For<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        T>(string hostUrl, RefitSettings? settings)
    {
        var client = CreateHttpClient(hostUrl, settings);

        return For<T>(client, settings);
    }

    /// <summary>Generate a Refit implementation of the specified interface.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="hostUrl">Base address the implementation will use.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresUnreferencedCode("Creating a generated client through the reflection path requires runtime type lookup and request metadata.")]
    public static T For<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        T>(string hostUrl) => For<T>(hostUrl, null);

    /// <summary>Generate a Refit implementation of the specified interface.</summary>
    /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="builder"><see cref="IRequestBuilder"/> to use to build requests.</param>
    /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
    [RequiresUnreferencedCode("Creating a generated client by Type requires runtime type lookup and constructor metadata.")]
    public static object For(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
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
            DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type refitInterfaceType,
        HttpClient client,
        RefitSettings? settings)
    {
        var resolvedSettings = settings ?? new();

        // A generated settings factory means every method builds its request inline, so the reflection
        // request builder (and the Refit.Reflection assembly) is never needed for this interface.
        if (TryResolveInlineClient(refitInterfaceType, client, resolvedSettings, _generatedRegistrationRunner, out var instance))
        {
            return instance;
        }

        var requestBuilder = RequestBuilder.ForType(refitInterfaceType, settings);

        return For(refitInterfaceType, client, requestBuilder);
    }

    /// <summary>Generate a Refit implementation of the specified interface.</summary>
    /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("Creating a generated client through the reflection path requires runtime type lookup and request metadata.")]
    public static object For(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
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
            DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("Creating a generated client through the reflection path requires runtime type lookup and request metadata.")]
    public static object For(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
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

    /// <summary>Runs the generated-factory registrations declared by the assembly that owns a Refit interface.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <remarks>
    /// The generator emits those registrations in a <c>[ModuleInitializer]</c> in the assembly that declares the
    /// interface. The runtime only promises to run a module initializer before the first static field read or method
    /// call in that module, and resolving a client only ever does <c>typeof(T)</c>, which is neither. CoreCLR runs
    /// module initializers eagerly regardless, but Mono - which Blazor WebAssembly runs on - does not, so a lookup can
    /// miss purely because the initializer has not run yet. Forcing the module constructor is safe to repeat, because
    /// a module constructor runs at most once.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RunGeneratedRegistrations(Type refitInterfaceType) =>
        RuntimeHelpers.RunModuleConstructor(refitInterfaceType.Module.ModuleHandle);

    /// <summary>Resolves a generated implementation, forcing the interface assembly's registrations on a miss.</summary>
    /// <typeparam name="T">The Refit interface type.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the generated client.</param>
    /// <param name="runRegistrations">Runs the registrations declared by the interface's assembly.</param>
    /// <param name="instance">The resolved implementation.</param>
    /// <returns><see langword="true"/> when a generated implementation was resolved.</returns>
    internal static bool TryResolveGeneratedClient<T>(
        HttpClient client,
        RefitSettings settings,
        Action<Type> runRegistrations,
        out T instance)
    {
        if (TryCreateGeneratedClient(client, settings, out instance))
        {
            return true;
        }

        runRegistrations(typeof(T));

        return TryCreateGeneratedClient(client, settings, out instance);
    }

    /// <summary>Resolves a generated implementation, forcing the interface assembly's registrations on a miss.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the generated client.</param>
    /// <param name="runRegistrations">Runs the registrations declared by the interface's assembly.</param>
    /// <param name="instance">The resolved implementation.</param>
    /// <returns><see langword="true"/> when a generated implementation was resolved.</returns>
    internal static bool TryResolveGeneratedClient(
        Type refitInterfaceType,
        HttpClient client,
        RefitSettings settings,
        Action<Type> runRegistrations,
        out object instance)
    {
        if (TryCreateGeneratedClient(refitInterfaceType, client, settings, out instance))
        {
            return true;
        }

        runRegistrations(refitInterfaceType);

        return TryCreateGeneratedClient(refitInterfaceType, client, settings, out instance);
    }

    /// <summary>Resolves a fully inline generated implementation, forcing the interface assembly's registrations on a miss.</summary>
    /// <typeparam name="T">The Refit interface type.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the generated client.</param>
    /// <param name="runRegistrations">Runs the registrations declared by the interface's assembly.</param>
    /// <param name="instance">The resolved implementation.</param>
    /// <returns><see langword="true"/> when an inline generated implementation was resolved.</returns>
    internal static bool TryResolveInlineClient<T>(
        HttpClient client,
        RefitSettings settings,
        Action<Type> runRegistrations,
        out T instance)
    {
        if (TryCreateInlineClient(client, settings, out instance))
        {
            return true;
        }

        runRegistrations(typeof(T));

        return TryCreateInlineClient(client, settings, out instance);
    }

    /// <summary>Resolves a fully inline generated implementation, forcing the interface assembly's registrations on a miss.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the generated client.</param>
    /// <param name="runRegistrations">Runs the registrations declared by the interface's assembly.</param>
    /// <param name="instance">The resolved implementation.</param>
    /// <returns><see langword="true"/> when an inline generated implementation was resolved.</returns>
    internal static bool TryResolveInlineClient(
        Type refitInterfaceType,
        HttpClient client,
        RefitSettings settings,
        Action<Type> runRegistrations,
        out object instance)
    {
        if (TryCreateInlineClient(refitInterfaceType, client, settings, out instance))
        {
            return true;
        }

        runRegistrations(refitInterfaceType);

        return TryCreateInlineClient(refitInterfaceType, client, settings, out instance);
    }

    /// <summary>Resolves the generated implementation type for a Refit interface.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <returns>The generated implementation type.</returns>
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    [RequiresUnreferencedCode("Resolving a generated client type by name requires runtime type lookup.")]
    internal static Type GetGeneratedType(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type refitInterfaceType)
    {
        var typeName = UniqueName.ForType(refitInterfaceType);

        return Type.GetType(typeName, false)
            ?? throw CreateMissingGeneratedFactoryException(refitInterfaceType);
    }

    /// <summary>Creates the exception thrown when no source-generated implementation is available.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <returns>The generated-client exception.</returns>
    internal static InvalidOperationException CreateMissingGeneratedFactoryException(Type refitInterfaceType)
    {
        var message = string.Concat(
            refitInterfaceType.Name,
            " doesn't look like a Refit interface. Make sure it has at least one method with a Refit HTTP method attribute, ",
            "the Refit source generator is installed in the project, and your build produced the generated client. ",
            "For Native AOT or trimmed apps, prefer generated clients plus source-generated System.Text.Json metadata.");

        return new(message);
    }

    /// <summary>Creates a generated implementation from the registered factories.</summary>
    /// <typeparam name="T">The Refit interface type.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the generated client.</param>
    /// <param name="instance">The created implementation.</param>
    /// <returns><see langword="true"/> when a factory was registered.</returns>
    private static bool TryCreateGeneratedClient<T>(HttpClient client, RefitSettings settings, out T instance)
    {
        if (TryCreateInlineClient(client, settings, out instance))
        {
            return true;
        }

        if (_generatedFactories.TryGetValue(typeof(T), out var untypedFactory))
        {
            instance = (T)untypedFactory(client, new GeneratedOnlyRequestBuilder(settings));
            return true;
        }

        if (GeneratedFactory<T>.Factory is { } factory)
        {
            instance = factory(client, new GeneratedOnlyRequestBuilder(settings));
            return true;
        }

        instance = default!;
        return false;
    }

    /// <summary>Creates a generated implementation from the registered factories.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the generated client.</param>
    /// <param name="instance">The created implementation.</param>
    /// <returns><see langword="true"/> when a factory was registered.</returns>
    private static bool TryCreateGeneratedClient(
        Type refitInterfaceType,
        HttpClient client,
        RefitSettings settings,
        out object instance)
    {
        if (TryCreateInlineClient(refitInterfaceType, client, settings, out instance))
        {
            return true;
        }

        if (_generatedFactories.TryGetValue(refitInterfaceType, out var factory))
        {
            instance = factory(client, new GeneratedOnlyRequestBuilder(settings));
            return true;
        }

        instance = default!;
        return false;
    }

    /// <summary>Creates a generated implementation whose methods all build their requests inline.</summary>
    /// <typeparam name="T">The Refit interface type.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the generated client.</param>
    /// <param name="instance">The created implementation.</param>
    /// <returns><see langword="true"/> when a settings factory was registered.</returns>
    private static bool TryCreateInlineClient<T>(HttpClient client, RefitSettings settings, out T instance)
    {
        if (GeneratedSettingsFactory<T>.Factory is { } settingsFactory)
        {
            instance = settingsFactory(client, settings);
            return true;
        }

        if (_generatedSettingsFactories.TryGetValue(typeof(T), out var untypedSettingsFactory))
        {
            instance = (T)untypedSettingsFactory(client, settings);
            return true;
        }

        instance = default!;
        return false;
    }

    /// <summary>Creates a generated implementation whose methods all build their requests inline.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the generated client.</param>
    /// <param name="instance">The created implementation.</param>
    /// <returns><see langword="true"/> when a settings factory was registered.</returns>
    private static bool TryCreateInlineClient(
        Type refitInterfaceType,
        HttpClient client,
        RefitSettings settings,
        out object instance)
    {
        if (_generatedSettingsFactories.TryGetValue(refitInterfaceType, out var settingsFactory))
        {
            instance = settingsFactory(client, settings);
            return true;
        }

        instance = default!;
        return false;
    }

#if !NET5_0_OR_GREATER
    /// <summary>Creates a generated implementation by resolving its emitted type name.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="settings"><see cref="RefitSettings"/> to use to configure the generated client.</param>
    /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
    /// <remarks>
    /// .NET Framework has no <c>[ModuleInitializer]</c>, so the generator emits no registrations for those targets and
    /// the factory registries stay empty there. Resolving the generated type by name is the same fallback the
    /// reflection path already uses, and it keeps the modern targets free of runtime type lookup.
    /// </remarks>
    private static object CreateByGeneratedTypeName(Type refitInterfaceType, HttpClient client, RefitSettings settings)
    {
        var generatedType = _typeMapping.GetOrAdd(refitInterfaceType, GetGeneratedType);

        return Activator.CreateInstance(generatedType, client, new GeneratedOnlyRequestBuilder(settings))!;
    }
#endif

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
