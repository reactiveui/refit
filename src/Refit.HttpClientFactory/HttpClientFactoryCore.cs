// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Refit;

/// <summary>Shared registration logic backing the Refit HttpClientFactory extension methods.</summary>
internal static class HttpClientFactoryCore
{
    /// <summary>Registers a strongly typed, source-generated Refit client without any reflection fallback.</summary>
    /// <typeparam name="T">The Refit interface type to register.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="settings">A factory that produces the Refit settings, or null.</param>
    /// <param name="httpClientName">A name for the underlying HTTP client, or null.</param>
    /// <returns>The HTTP client builder for further configuration.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
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

    /// <summary>Registers a strongly typed, keyed, source-generated Refit client without any reflection fallback.</summary>
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
    internal static IHttpClientBuilder AddKeyedRefitGeneratedClientCore<T>(
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

        // create HttpClientBuilder
        var builder = services.AddHttpClient(httpClientName ?? UniqueName.ForType<T>(serviceKey));

        // configure primary and additional handlers from the keyed settings
        ConfigureKeyedHandlers(
            builder,
            serviceProvider => serviceProvider.GetRequiredKeyedService<SettingsFor<T>>(serviceKey));

        // add keyed typed client backed by the source generator only; throws clearly if no generated client exists
        _ = builder.Services.AddKeyedTransient(
            serviceKey,
            (s, _) =>
            {
                var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(builder.Name);
                return RestService.ForGenerated<T>(
                    httpClient,
                    s.GetRequiredKeyedService<SettingsFor<T>>(serviceKey).Settings ?? new RefitSettings());
            });

        return builder;
    }

#if NET8_0_OR_GREATER
    /// <summary>Wraps a settings factory so the settings it produces run on a source-generated JSON context.</summary>
    /// <param name="settings">A factory that produces the Refit settings, or null.</param>
    /// <param name="context">The generated context to register.</param>
    /// <param name="allowReflectionFallback">Whether a type no resolver describes may use reflection-based JSON.</param>
    /// <returns>A factory whose settings keep the serializer options they came with and gain the context, or use the context's own options when the factory yields no settings.</returns>
    internal static Func<IServiceProvider, RefitSettings?> WithJsonContext(
        Func<IServiceProvider, RefitSettings?>? settings,
        System.Text.Json.Serialization.JsonSerializerContext context,
        bool allowReflectionFallback)
    {
        ArgumentExceptionHelper.ThrowIfNull(context);

        return provider => settings?.Invoke(provider) is { } supplied
            ? supplied.UseJsonContext(context, allowReflectionFallback)
            : RefitSettings.ForJsonContext(context, allowReflectionFallback);
    }

#endif
    /// <summary>Configures the primary and authorization handlers for a keyed Refit client from its settings.</summary>
    /// <param name="builder">The HTTP client builder to configure.</param>
    /// <param name="settingsResolver">Resolves the settings holder for the keyed client from a service provider.</param>
    internal static void ConfigureKeyedHandlers(
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
    internal static HttpMessageHandler? CreateInnerHandlerIfProvided(RefitSettings? settings)
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
