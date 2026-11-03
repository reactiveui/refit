// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Refit;

/// <summary>Opt-in converters for <see cref="JsonSerializerOptions"/> that Refit's core assembly does not add, because converters disable the System.Text.Json source-generated fast-path.</summary>
public static class ReflectionJsonSerializerOptionsExtensions
{
    /// <summary>Extensions on the JSON serializer options being configured.</summary>
    /// <param name="options">The options to add the converters to.</param>
    extension(JsonSerializerOptions options)
    {
        /// <summary>
        /// Adds <see cref="ObjectToInferredTypesConverter"/> and a camelCase <see cref="JsonStringEnumConverter"/>,
        /// so <see cref="object"/> values read as CLR primitives and enums are written and read as camelCase names.
        /// </summary>
        /// <returns>The same options instance.</returns>
        /// <remarks>
        /// Using these options disables the System.Text.Json fast-path. Prefer <c>UseStringEnumConverter</c> on a
        /// source-generated <see cref="JsonSerializerContext"/> where a context is available.
        /// </remarks>
        public JsonSerializerOptions AddReflectionConverters()
        {
            ArgumentExceptionHelper.ThrowIfNull(options);

            options.Converters.Add(new ObjectToInferredTypesConverter());
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            return options;
        }
    }
}
