// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Refit;

/// <summary>
/// ObjectToInferredTypesConverter.
/// From https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-5-0#deserialize-inferred-types-to-object-properties.
/// </summary>
public class ObjectToInferredTypesConverter : JsonConverter<object>
{
    /// <summary>Reads and converts the JSON to type typeToConvert.</summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    /// <returns>
    /// The converted value.
    /// </returns>
    public override object? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt64(out var l) => l,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String when reader.TryGetDateTime(out var datetime) => datetime,
            JsonTokenType.String => reader.GetString(),
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
        };

    /// <summary>Writes the specified writer.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The object to write.</param>
    /// <param name="options">The options.</param>
    public override void Write(
        Utf8JsonWriter writer,
        object value,
        JsonSerializerOptions options)
    {
        var runtimeType = value.GetType();

        // A bare System.Object has no properties to serialize. Serializing it by its
        // runtime type would re-enter this converter (registered for object) and recurse
        // until the stack overflows, so emit an empty JSON object directly.
        if (runtimeType == typeof(object))
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
            return;
        }

#if NET8_0_OR_GREATER
        JsonSerializer.Serialize(writer, value, options.GetTypeInfo(runtimeType));
#else
        JsonSerializer.Serialize(writer, value, runtimeType, options);
#endif
    }
}
