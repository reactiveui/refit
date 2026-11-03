// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Refit;

/// <summary>Registration logic backing the reflection-based Refit HttpClientFactory extension methods.</summary>
internal static class HttpClientFactoryReflectionCore
{
    /// <summary>Lazily cached generic RequestBuilder.ForType method used to build request builders by type.</summary>
    private static MethodInfo? _requestBuilderGenericForTypeMethod;

    /// <summary>Registers a Refit client and its dependencies for the given interface type.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="refitInterfaceType">The Refit interface type to register.</param>
    /// <param name="settings">A factory that produces the Refit settings, or null.</param>
    /// <param name="httpClientName">A name for the underlying HTTP client, or null.</param>
    /// <returns>The HTTP client builder for further configuration.</returns>
    internal static IHttpClientBuilder AddRefitClientCore(
        IServiceCollection services,
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
                typeof(SettingsFor<>).MakeGenericType(refitInterfaceType),
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
            HttpClientFactoryCore.CreateInnerHandlerIfProvided(
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
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
    internal static IHttpClientBuilder AddRefitClientCore<
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
            HttpClientFactoryCore.CreateInnerHandlerIfProvided(
                serviceProvider.GetRequiredService<SettingsFor<T>>().Settings)
            ?? new HttpClientHandler());

        // add typed client using framework AddTypedClient
        return builder.AddTypedClient(static (client, serviceProvider) =>
            RestService.For(
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
    internal static IHttpClientBuilder AddKeyedRefitClientCore(
        IServiceCollection services,
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
                typeof(SettingsFor<>).MakeGenericType(refitInterfaceType),
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
        HttpClientFactoryCore.ConfigureKeyedHandlers(
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
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
    internal static IHttpClientBuilder AddKeyedRefitClientCore<
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
        HttpClientFactoryCore.ConfigureKeyedHandlers(
            builder,
            serviceProvider => serviceProvider.GetRequiredKeyedService<SettingsFor<T>>(serviceKey));

        // add keyed typed client (inline keyed registration)
        _ = builder.Services.AddKeyedTransient(
            serviceKey,
            (s, _) =>
            {
                var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(builder.Name);
                return RestService.For(
                    httpClient,
                    s.GetRequiredKeyedService<IRequestBuilder<T>>(serviceKey));
            });

        return builder;
    }

    /// <summary>Resolves and caches the open generic <see cref="RequestBuilder.ForType{T}(RefitSettings?)"/> method.</summary>
    /// <returns>The open generic <c>RequestBuilder.ForType</c> method definition.</returns>
    private static MethodInfo GetRequestBuilderGenericForTypeMethod() =>
        _requestBuilderGenericForTypeMethod ??= FindRequestBuilderGenericForTypeMethod();

    /// <summary>Finds the open generic <see cref="RequestBuilder.ForType{T}(RefitSettings?)"/> method.</summary>
    /// <returns>The matching method definition.</returns>
    /// <exception cref="InvalidOperationException"><see cref="RequestBuilder"/> declares no single-parameter generic <c>ForType</c> method, or more than one.</exception>
    [ExcludeFromCodeCoverage] // RequestBuilder declares exactly one single-parameter generic ForType, so the duplicate-match and no-match guards are unreachable.
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
}
