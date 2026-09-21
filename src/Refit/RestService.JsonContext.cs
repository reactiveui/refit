// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Refit;

/// <summary>Creates source-generated Refit clients that run on a source-generated JSON context.</summary>
public static partial class RestService
{
    /// <summary>Create a source-generated Refit implementation that reads and writes JSON with a source-generated context and never falls back to reflection.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="context">The generated context. Its own options supply the naming, converters and number handling.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <typeparamref name="T"/>.</exception>
    /// <exception cref="NotSupportedException">Thrown when a request or reply uses a type the context does not describe.</exception>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public static T ForGenerated<T>(HttpClient client, JsonSerializerContext context) =>
        ForGenerated<T>(client, RefitSettings.ForJsonContext(context));

    /// <summary>Create a source-generated Refit implementation that reads and writes JSON with a source-generated context.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="context">The generated context. Its own options supply the naming, converters and number handling.</param>
    /// <param name="allowReflectionFallback"><see langword="true"/> to let a type the context does not describe use reflection-based JSON. Reflection is not trim or Native AOT safe.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <typeparamref name="T"/>.</exception>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public static T ForGenerated<T>(
        HttpClient client,
        JsonSerializerContext context,
        bool allowReflectionFallback) =>
        ForGenerated<T>(client, RefitSettings.ForJsonContext(context, allowReflectionFallback));

    /// <summary>Create a source-generated Refit implementation that keeps the settings' serializer options and adds a source-generated JSON context.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="context">The generated context that supplies metadata for the types it describes.</param>
    /// <param name="settings">The settings to use. Their serializer must be a <see cref="SystemTextJsonContentSerializer"/>, which gains <paramref name="context"/>.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <typeparamref name="T"/>, or the settings do not use System.Text.Json.</exception>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public static T ForGenerated<T>(
        HttpClient client,
        JsonSerializerContext context,
        RefitSettings settings)
    {
        ArgumentExceptionHelper.ThrowIfNull(settings);

        return ForGenerated<T>(client, settings.UseJsonContext(context));
    }

    /// <summary>Create a source-generated Refit implementation that keeps the settings' serializer options and adds a source-generated JSON context.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
    /// <param name="context">The generated context that supplies metadata for the types it describes.</param>
    /// <param name="settings">The settings to use. Their serializer must be a <see cref="SystemTextJsonContentSerializer"/>, which gains <paramref name="context"/>.</param>
    /// <param name="allowReflectionFallback"><see langword="true"/> to let a type no resolver describes use reflection-based JSON. Reflection is not trim or Native AOT safe.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <typeparamref name="T"/>, or the settings do not use System.Text.Json.</exception>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public static T ForGenerated<T>(
        HttpClient client,
        JsonSerializerContext context,
        RefitSettings settings,
        bool allowReflectionFallback)
    {
        ArgumentExceptionHelper.ThrowIfNull(settings);

        return ForGenerated<T>(client, settings.UseJsonContext(context, allowReflectionFallback));
    }

    /// <summary>Create a source-generated Refit implementation that reads and writes JSON with a source-generated context and never falls back to reflection.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="hostUrl">Base address the implementation will use.</param>
    /// <param name="context">The generated context. Its own options supply the naming, converters and number handling.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <typeparamref name="T"/>.</exception>
    /// <exception cref="NotSupportedException">Thrown when a request or reply uses a type the context does not describe.</exception>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public static T ForGenerated<T>(string hostUrl, JsonSerializerContext context) =>
        ForGenerated<T>(hostUrl, RefitSettings.ForJsonContext(context));

    /// <summary>Create a source-generated Refit implementation that reads and writes JSON with a source-generated context.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="hostUrl">Base address the implementation will use.</param>
    /// <param name="context">The generated context. Its own options supply the naming, converters and number handling.</param>
    /// <param name="allowReflectionFallback"><see langword="true"/> to let a type the context does not describe use reflection-based JSON. Reflection is not trim or Native AOT safe.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <typeparamref name="T"/>.</exception>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public static T ForGenerated<T>(
        string hostUrl,
        JsonSerializerContext context,
        bool allowReflectionFallback) =>
        ForGenerated<T>(hostUrl, RefitSettings.ForJsonContext(context, allowReflectionFallback));

    /// <summary>Create a source-generated Refit implementation that keeps the settings' serializer options and adds a source-generated JSON context.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="hostUrl">Base address the implementation will use.</param>
    /// <param name="context">The generated context that supplies metadata for the types it describes.</param>
    /// <param name="settings">The settings to use. Their serializer must be a <see cref="SystemTextJsonContentSerializer"/>, which gains <paramref name="context"/>.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <typeparamref name="T"/>, or the settings do not use System.Text.Json.</exception>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public static T ForGenerated<T>(
        string hostUrl,
        JsonSerializerContext context,
        RefitSettings settings)
    {
        ArgumentExceptionHelper.ThrowIfNull(settings);

        return ForGenerated<T>(hostUrl, settings.UseJsonContext(context));
    }

    /// <summary>Create a source-generated Refit implementation that keeps the settings' serializer options and adds a source-generated JSON context.</summary>
    /// <typeparam name="T">Interface to create the implementation for.</typeparam>
    /// <param name="hostUrl">Base address the implementation will use.</param>
    /// <param name="context">The generated context that supplies metadata for the types it describes.</param>
    /// <param name="settings">The settings to use. Their serializer must be a <see cref="SystemTextJsonContentSerializer"/>, which gains <paramref name="context"/>.</param>
    /// <param name="allowReflectionFallback"><see langword="true"/> to let a type no resolver describes use reflection-based JSON. Reflection is not trim or Native AOT safe.</param>
    /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no generated implementation is registered for <typeparamref name="T"/>, or the settings do not use System.Text.Json.</exception>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public static T ForGenerated<T>(
        string hostUrl,
        JsonSerializerContext context,
        RefitSettings settings,
        bool allowReflectionFallback)
    {
        ArgumentExceptionHelper.ThrowIfNull(settings);

        return ForGenerated<T>(hostUrl, settings.UseJsonContext(context, allowReflectionFallback));
    }
}
#endif
