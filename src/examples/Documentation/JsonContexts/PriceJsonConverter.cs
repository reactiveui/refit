// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Refit.Documentation.JsonContexts;

/// <summary>Reads and writes a price as text with two decimal places, such as <c>"49.50"</c>.</summary>
internal sealed class PriceJsonConverter : JsonConverter<decimal>
{
    /// <summary>The format that keeps two decimal places.</summary>
    private const string PriceFormat = "F2";

    /// <inheritdoc />
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        decimal.Parse(reader.GetString()!, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(PriceFormat, CultureInfo.InvariantCulture));
}
