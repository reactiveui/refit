// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Refit;

/// <summary>Shared registration logic backing the Refit HttpClientFactory extension methods.</summary>
internal static class HttpClientFactoryCore
{
    /// <summary>Diagnostic message explaining why the reflection-based registration path is not trim-safe.</summary>
    private const string RequiresUnreferencedCodeMessage =
        "Registering Refit clients with HttpClientFactory requires reflected interface and request metadata.";

    /// <summary>Diagnostic message explaining why the reflection-based registration path is not AOT-safe.</summary>
    private const string RequiresDynamicCodeMessage =
        "Registering Refit clients by Type with HttpClientFactory requires runtime generic type and method instantiation.";

    /// <summary>Lazily cached generic RequestBuilder.ForType method used to build request builders by type.</summary>
    private static MethodInfo? _requestBuilderGenericForTypeMethod;

    /// <summary>Registers a Refit client and its dependencies for the given interface type.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="refitInterfaceType">The Refit interface type to register.</param>
    /// <param name="settings">A factory that produces the Refit settings, or null.</param>
    /// <param name="httpClientName">A name for the underlying HTTP client, or null.</param>
    /// <returns>The HTTP client builder for further configuration.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    internal static IHttpClientBuilder AddRefitClientCore(
        IServiceCollection services,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type refitInterfaceType,
        Func<IServiceProvider, RefitSettings?>? settings,
        string? httpClientName)
    {
        ArgumentExceptionHelper.ThrowIfNull(services);

        ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

        // register settings
        var settingsType = typeof(SettingsFor<>).MakeGenericType(refitInterfaceType);
        _ = services.AddSingleton(
            settingsType,
            provider => Activator.CreateInstance(
                typeof(SettingsFor<>).MakeGenericType(refitInterfaceType)!,
                settings?.Invoke(provider))!);

        // register RequestBuilder
        var requestBuilderType = typeof(IRequestBuilder<>).MakeGenericType(refitInterfaceType);
        _ = services.AddSingleton(
            requestBuilderType,
            provider => GetRequestBuilderGenericForTypeMethod()
                .MakeGenericMethod(refitInterfaceType)
                .Invoke(
                    null,
                    [((ISettingsFor)provider.GetRequiredService(settingsType)).Settings])!);

        // create HttpClientBuilder
        var builder = services.AddHttpClient(httpClientName ?? UniqueName.ForType(refitInterfaceType));

        // configure the primary handler from the supplied settings (or fall back to the default)
        _ = builder.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            CreateInnerHandlerIfProvided(
                ((ISettingsFor)serviceProvider.GetRequiredService(settingsType)).Settings)
            ?? new HttpClientHandler());

        // add typed client (register transient that resolves HttpClient from IHttpClientFactory and creates Refit client)
        _ = builder.Services.AddTransient(
            refitInterfaceType,
            s =>
            {
                var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(builder.Name);
                return RestService.For(
                    refitInterfaceType,
                    httpClient,
                    (IRequestBuilder)s.GetRequiredService(requestBuilderType));
            });

        return builder;
    }

    /// <summary>Registers a strongly typed Refit client and its dependencies.</summary>
    /// <typeparam name="T">The Refit interface type to register.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="settings">A factory that produces the Refit settings, or null.</param>
    /// <param name="httpClientName">A name for the underlying HTTP client, or null.</param>
    /// <returns>The HTTP client builder for further configuration.</returns>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    internal static IHttpClientBuilder AddRefitClientCore<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        T>(
        IServiceCollection services,
        Func<IServiceProvider, RefitSettings?>? settings,
        string? httpClientName)
        where T : class
    {
        ArgumentExceptionHelper.ThrowIfNull(services);

        // register settings
        _ = services.AddSingleton(provider => new SettingsFor<T>(settings?.Invoke(provider)));

        // register RequestBuilder
        _ = services.AddSingleton(static provider =>
            RequestBuilder.ForType<T>(provider.GetRequiredService<SettingsFor<T>>().Settings));

        // create HttpClientBuilder
        var builder = services.AddHttpClient(httpClientName ?? UniqueName.ForType<T>());

        // configure the primary handler from the supplied settings (or fall back to the default)
        _ = builder.ConfigurePrimaryHttpMessageHandler(static serviceProvider =>
            CreateInnerHandlerIfProvided(
                serviceProvider.GetRequiredService<SettingsFor<T>>().Settings)
            ?? new HttpClientHandler());

        // add typed client using framework AddTypedClient
        return builder.AddTypedClient(static (client, serviceProvider) =>
            RestService.For<T>(
                client,
                serviceProvider.GetRequiredService<IRequestBuilder<T>>()));
    }

    /// <summary>Registers a keyed Refit client and its dependencies for the given interface type.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="refitInterfaceType">The Refit interface type to register.</param>
    /// <param name="serviceKey">The key under which the client is registered.</param>
    /// <param name="settings">A factory that produces the Refit settings, or null.</param>
    /// <param name="httpClientName">A name for the underlying HTTP client, or null.</param>
    /// <returns>The HTTP client builder for further configuration.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    internal static IHttpClientBuilder AddKeyedRefitClientCore(
        IServiceCollection services,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type refitInterfaceType,
        object? serviceKey,
        Func<IServiceProvider, RefitSettings?>? settings,
        string? httpClientName)
    {
        ArgumentExceptionHelper.ThrowIfNull(services);

        ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

        ArgumentExceptionHelper.ThrowIfNull(serviceKey);

        // register settings
        var settingsType = typeof(SettingsFor<>).MakeGenericType(refitInterfaceType);
        _ = services.AddKeyedSingleton(
            settingsType,
            serviceKey,
            (provider, _) => Activator.CreateInstance(
                typeof(SettingsFor<>).MakeGenericType(refitInterfaceType)!,
                settings?.Invoke(provider))!);

        // register RequestBuilder
        var requestBuilderType = typeof(IRequestBuilder<>).MakeGenericType(refitInterfaceType);
        _ = services.AddKeyedSingleton(
            requestBuilderType,
            serviceKey,
            (provider, _) => GetRequestBuilderGenericForTypeMethod()
                .MakeGenericMethod(refitInterfaceType)
                .Invoke(
                    null,
                    [((ISettingsFor)provider.GetRequiredKeyedService(settingsType, serviceKey)).Settings])!);

        // create HttpClientBuilder
        var builder = services.AddHttpClient(httpClientName ?? UniqueName.ForType(refitInterfaceType, serviceKey));

        // configure primary and additional handlers from the keyed settings
        ConfigureKeyedHandlers(
            builder,
            serviceProvider => (ISettingsFor)serviceProvider.GetRequiredKeyedService(settingsType, serviceKey));

        // add keyed typed client (register keyed transient that resolves HttpClient and creates Refit client)
        _ = builder.Services.AddKeyedTransient(
            refitInterfaceType,
            serviceKey,
            (s, _) =>
            {
                var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(builder.Name);
                return RestService.For(
                    refitInterfaceType,
                    httpClient,
                    (IRequestBuilder)s.GetRequiredKeyedService(requestBuilderType, serviceKey));
            });

        return builder;
    }

    /// <summary>Registers a strongly typed keyed Refit client and its dependencies.</summary>
    /// <typeparam name="T">The Refit interface type to register.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="serviceKey">The key under which the client is registered.</param>
    /// <param name="settings">A factory that produces the Refit settings, or null.</param>
    /// <param name="httpClientName">A name for the underlying HTTP client, or null.</param>
    /// <returns>The HTTP client builder for further configuration.</returns>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    internal static IHttpClientBuilder AddKeyedRefitClientCore<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        T>(
        IServiceCollection services,
        object? serviceKey,
        Func<IServiceProvider, RefitSettings?>? settings,
        string? httpClientName)
        where T : class
    {
        ArgumentExceptionHelper.ThrowIfNull(services);

        ArgumentExceptionHelper.ThrowIfNull(serviceKey);

        // register settings
        _ = services.AddKeyedSingleton(
            serviceKey,
            (provider, _) => new SettingsFor<T>(settings?.Invoke(provider)));

        // register RequestBuilder
        _ = services.AddKeyedSingleton(
            serviceKey,
            (provider, _) =>
                RequestBuilder.ForType<T>(
                    provider.GetRequiredKeyedService<SettingsFor<T>>(serviceKey).Settings));

        // create HttpClientBuilder
        var builder = services.AddHttpClient(httpClientName ?? UniqueName.ForType<T>(serviceKey));

        // configure primary and additional handlers from the keyed settings
        ConfigureKeyedHandlers(
            builder,
            serviceProvider => serviceProvider.GetRequiredKeyedService<SettingsFor<T>>(serviceKey));

        // add keyed typed client (inline keyed registration)
        _ = builder.Services.AddKeyedTransient(
            serviceKey,
            (s, _) =>
            {
                var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(builder.Name);
                return RestService.For<T>(
                    httpClient,
                    s.GetRequiredKeyedService<IRequestBuilder<T>>(serviceKey));
            });

        return builder;
    }

    /// <summary>Registers a strongly typed, source-generated Refit client without any reflection fallback.</summary>
    /// <typeparam name="T">The Refit interface type to register.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="settings">A factory that produces the Refit settings, or null.</param>
    /// <param name="httpClientName">A name for the underlying HTTP client, or null.</param>
    /// <returns>The HTTP client builder for further configuration.</returns>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
    internal static IHttpClientBuilder AddRefitGeneratedClientCore<T>(
        IServiceCollection services,
        Func<IServiceProvider, RefitSettings?>? settings,
        string? httpClientName)
        where T : class
    {
        ArgumentExceptionHelper.ThrowIfNull(services);

        // register settings
        _ = services.AddSingleton(provider => new SettingsFor<T>(settings?.Invoke(provider)));

        // create HttpClientBuilder
        var builder = services.AddHttpClient(httpClientName ?? UniqueName.ForType<T>());

        // configure the primary handler from the supplied settings (or fall back to the default)
        _ = builder.ConfigurePrimaryHttpMessageHandler(static serviceProvider =>
            CreateInnerHandlerIfProvided(
                serviceProvider.GetRequiredService<SettingsFor<T>>().Settings)
            ?? new HttpClientHandler());

        // add typed client backed by the source generator only; throws clearly if no generated client exists
        return builder.AddTypedClient(static (client, serviceProvider) =>
            RestService.ForGenerated<T>(
                client,
                serviceProvider.GetRequiredService<SettingsFor<T>>().Settings ?? new RefitSettings()));
    }

    /// <summary>Resolves and caches the open generic <see cref="RequestBuilder.ForType{T}(RefitSettings?)"/> method.</summary>
    /// <returns>The open generic <c>RequestBuilder.ForType</c> method definition.</returns>
    [RequiresUnreferencedCode("Resolving RequestBuilder.ForType by reflection requires method metadata to be available at runtime.")]
    private static MethodInfo GetRequestBuilderGenericForTypeMethod() =>
        _requestBuilderGenericForTypeMethod ??= FindRequestBuilderGenericForTypeMethod();

    /// <summary>Finds the open generic <see cref="RequestBuilder.ForType{T}(RefitSettings?)"/> method.</summary>
    /// <returns>The matching method definition.</returns>
    [RequiresUnreferencedCode("Resolving RequestBuilder.ForType by reflection requires method metadata to be available at runtime.")]
    private static MethodInfo FindRequestBuilderGenericForTypeMethod()
    {
        var methods = typeof(RequestBuilder).GetMethods(BindingFlags.Public | BindingFlags.Static);
        MethodInfo? match = null;
        for (var i = 0; i < methods.Length; i++)
        {
            var method = methods[i];
            if (!method.IsGenericMethodDefinition || method.GetParameters().Length != 1)
            {
                continue;
            }

            if (match is not null)
            {
                throw new InvalidOperationException("Sequence contains more than one matching element");
            }

            match = method;
        }

        return match ?? throw new InvalidOperationException("Sequence contains no matching element");
    }

    /// <summary>Configures the primary and authorization handlers for a keyed Refit client from its settings.</summary>
    /// <param name="builder">The HTTP client builder to configure.</param>
    /// <param name="settingsResolver">Resolves the settings holder for the keyed client from a service provider.</param>
    private static void ConfigureKeyedHandlers(
        IHttpClientBuilder builder,
        Func<IServiceProvider, ISettingsFor> settingsResolver)
    {
        _ = builder.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            settingsResolver(serviceProvider).Settings?.HttpMessageHandlerFactory?.Invoke() ?? new HttpClientHandler());

        _ = builder.ConfigureAdditionalHttpMessageHandlers((handlers, serviceProvider) =>
        {
            if (settingsResolver(serviceProvider).Settings?.AuthorizationHeaderValueGetter is not { } getToken)
            {
                return;
            }

            handlers.Add(new AuthenticatedHttpClientHandler(null, getToken));
        });
    }

    /// <summary>Builds the inner HTTP message handler from the supplied settings, if any.</summary>
    /// <param name="settings">The Refit settings that may provide a handler factory or auth getter.</param>
    /// <returns>The configured inner handler, or null when none is required.</returns>
    private static HttpMessageHandler? CreateInnerHandlerIfProvided(RefitSettings? settings)
    {
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

        return innerHandler;
    }
}
