// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NET8_0_OR_GREATER
using System.Text.Json.Serialization;

namespace Refit;

/// <summary>Registration of a source-generated <see cref="JsonSerializerContext"/> on Refit settings.</summary>
public partial class RefitSettings
{
    /// <summary>Creates settings that run on the options of a source-generated context, with reflection-based JSON disabled.</summary>
    /// <param name="context">The generated context. Its own options supply the naming, converters and number handling.</param>
    /// <returns>New settings whose content serializer is <see cref="SystemTextJsonContentSerializer.ForContext(JsonSerializerContext)"/>.</returns>
    public static RefitSettings ForJsonContext(JsonSerializerContext context) =>
        new(SystemTextJsonContentSerializer.ForContext(context));

    /// <summary>Creates settings that run on the options of a source-generated context, optionally with a reflection fallback.</summary>
    /// <param name="context">The generated context. Its own options supply the naming, converters and number handling.</param>
    /// <param name="allowReflectionFallback"><see langword="true"/> to let a type the context does not describe use reflection-based JSON. Reflection is not trim or Native AOT safe.</param>
    /// <returns>New settings whose content serializer is <see cref="SystemTextJsonContentSerializer.ForContext(JsonSerializerContext, bool)"/>.</returns>
    public static RefitSettings ForJsonContext(JsonSerializerContext context, bool allowReflectionFallback) =>
        new(SystemTextJsonContentSerializer.ForContext(context, allowReflectionFallback));

    /// <summary>Adds a source-generated context to these settings' System.Text.Json serializer, with reflection-based JSON disabled.</summary>
    /// <param name="context">The generated context that supplies metadata for the types it describes.</param>
    /// <returns>These settings.</returns>
    /// <exception cref="InvalidOperationException"><see cref="ContentSerializer"/> is not a <see cref="SystemTextJsonContentSerializer"/>.</exception>
    /// <remarks>
    /// The serializer's naming policy, converters and resolvers stay in place. Only <see cref="ContentSerializer"/> changes,
    /// to a serializer built on a copy of the current options.
    /// </remarks>
    public RefitSettings UseJsonContext(JsonSerializerContext context) =>
        UseJsonContext(context, false);

    /// <summary>Adds a source-generated context to these settings' System.Text.Json serializer, optionally with a reflection fallback.</summary>
    /// <param name="context">The generated context that supplies metadata for the types it describes.</param>
    /// <param name="allowReflectionFallback"><see langword="true"/> to let a type no resolver describes use reflection-based JSON. Reflection is not trim or Native AOT safe.</param>
    /// <returns>These settings.</returns>
    /// <exception cref="InvalidOperationException"><see cref="ContentSerializer"/> is not a <see cref="SystemTextJsonContentSerializer"/>.</exception>
    public RefitSettings UseJsonContext(JsonSerializerContext context, bool allowReflectionFallback)
    {
        ArgumentExceptionHelper.ThrowIfNull(context);

        ContentSerializer = ContentSerializer is SystemTextJsonContentSerializer serializer
            ? serializer.WithContext(context, allowReflectionFallback)
            : throw new InvalidOperationException(
                "A JSON serializer context can only be added to settings that use SystemTextJsonContentSerializer.");

        return this;
    }
}
#endif
