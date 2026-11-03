// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Refit.Tests;

/// <summary>Verifies the opt-in converter that infers CLR primitives for <see cref="object"/> values.</summary>
public sealed class ObjectToInferredTypesConverterTests
{
    /// <summary>The 64-bit value of the integral token used by the read tests.</summary>
    private const long IntegralValue = 7L;

    /// <summary>The floating-point value of the fractional token used by the read tests.</summary>
    private const double FloatingValue = 1.5;

    /// <summary>The options handed to the converter; the write path resolves runtime types through their resolver.</summary>
    private static readonly JsonSerializerOptions Options = new() { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

    /// <summary>Verifies each JSON token infers the expected CLR type and value.</summary>
    /// <param name="json">The JSON token to read.</param>
    /// <param name="expected">The expected value.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [Arguments("true", true)]
    [Arguments("false", false)]
    [Arguments("7", IntegralValue)]
    [Arguments("1.5", FloatingValue)]
    [Arguments("\"Ada\"", "Ada")]
    public async Task ReadInfersPrimitiveTokens(string json, object expected)
    {
        var value = Read(json);

        await Assert.That(value).IsEqualTo(expected);
        await Assert.That(value!.GetType()).IsEqualTo(expected.GetType());
    }

    /// <summary>Verifies a date string infers <see cref="DateTime"/>.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ReadInfersDateStrings()
    {
        var value = Read("\"2026-01-02T03:04:05\"");

        await Assert.That(value).IsTypeOf<DateTime>();
    }

    /// <summary>Verifies structured tokens stay <see cref="JsonElement"/>.</summary>
    /// <param name="json">The JSON token to read.</param>
    /// <param name="kind">The expected value kind.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [Arguments("{}", JsonValueKind.Object)]
    [Arguments("[]", JsonValueKind.Array)]
    [Arguments("null", JsonValueKind.Null)]
    public async Task ReadKeepsStructuredTokensAsJsonElements(string json, JsonValueKind kind)
    {
        var value = Read(json);

        await Assert.That(await Assert.That(value).IsTypeOf<JsonElement>()).IsNotDefault();
        await Assert.That(((JsonElement)value!).ValueKind).IsEqualTo(kind);
    }

    /// <summary>Verifies a bare object writes as an empty JSON object instead of recursing.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task WriteEmitsEmptyObjectForBareObject() =>
        await Assert.That(Write(new())).IsEqualTo("{}");

    /// <summary>Verifies other values write by their runtime type.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task WriteSerializesByRuntimeType()
    {
        await Assert.That(Write("Ada")).IsEqualTo("\"Ada\"");
        await Assert.That(Write(IntegralValue)).IsEqualTo("7");
    }

    /// <summary>Reads one JSON token through the converter.</summary>
    /// <param name="json">The JSON token.</param>
    /// <returns>The inferred value.</returns>
    private static object? Read(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        _ = reader.Read();
        return new ObjectToInferredTypesConverter().Read(ref reader, typeof(object), Options);
    }

    /// <summary>Writes one value through the converter.</summary>
    /// <param name="value">The value to write.</param>
    /// <returns>The JSON text.</returns>
    private static string Write(object value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            new ObjectToInferredTypesConverter().Write(writer, value, Options);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
