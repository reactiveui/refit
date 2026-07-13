// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SystemTextJsonSerializer = System.Text.Json.JsonSerializer;

namespace Refit.Tests;

/// <summary>Verifies how the System.Text.Json content serializer converts JSON values and enums: object-value inference, number handling, and enum serialization and deserialization.</summary>
public partial class SerializedContentTests
{
    /// <summary>Verifies that boolean JSON object values are inferred as <see cref="bool"/>.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_InferBooleanObjectValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>(
            """{"value":true}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(await Assert.That(result!.Value).IsTypeOf<bool>()).IsTrue();
    }

#if NET10_0_OR_GREATER
    /// <summary>Verifies duplicate JSON properties are rejected by the default serializer options.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_RejectDuplicateProperties()
    {
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();

        await Assert
            .That(() => SystemTextJsonSerializer.Deserialize<ObjectValueContainer>("""{"value":1,"value":2}""", options))
            .ThrowsExactly<JsonException>();
    }

#endif

    /// <summary>Verifies false JSON object values are inferred as <see cref="bool"/>.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_InferFalseObjectValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>(
            """{"value":false}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(await Assert.That(result!.Value).IsTypeOf<bool>()).IsFalse();
    }

    /// <summary>Verifies that integral JSON object values are inferred as <see cref="long"/>.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_InferIntegralObjectValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>(
            """{"value":42}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(await Assert.That(result!.Value).IsTypeOf<long>()).IsEqualTo(ExpectedIntegralValue);
    }

    /// <summary>Verifies that floating-point JSON object values are inferred as <see cref="double"/>.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_InferFloatingPointObjectValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>(
            """{"value":42.5}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(await Assert.That(result!.Value).IsTypeOf<double>()).IsEqualTo(ExpectedFloatingPointValue);
    }

    /// <summary>Verifies the default options read numeric properties from JSON strings.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_ReadNumbersFromString()
    {
        var result = SystemTextJsonSerializer.Deserialize<NumberContainer>(
            """{"id":"123","amount":"9.99"}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(result!.Id).IsEqualTo(ExpectedId);
        await Assert.That(result.Amount).IsEqualTo(ExpectedAmount);
    }

    /// <summary>Verifies the default options still write numbers as JSON numbers, not strings.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_WriteNumbersAsNumbers()
    {
        var json = SystemTextJsonSerializer.Serialize(
            new NumberContainer { Id = ExpectedId, Amount = ExpectedAmount },
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(json).Contains("123", StringComparison.Ordinal);
        await Assert.That(json).DoesNotContain("\"123\"", StringComparison.Ordinal);
    }

    /// <summary>Verifies the default options expose <see cref="JsonNumberHandling.AllowReadingFromString"/>.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_NumberHandlingAllowsReadingFromString() =>
        await Assert
            .That(SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions().NumberHandling)
            .IsEqualTo(JsonNumberHandling.AllowReadingFromString);

    /// <summary>Verifies the fast-path options omit the settings that disable the System.Text.Json fast-path.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_FastPathOptions_AreFastPathEligible()
    {
        var options = SystemTextJsonContentSerializer.GetFastPathJsonSerializerOptions();

        await Assert.That(options.Converters).IsEmpty();
        await Assert.That(options.NumberHandling).IsEqualTo(JsonNumberHandling.Strict);
        await Assert.That(options.ReferenceHandler).IsNull();
        await Assert.That(options.Encoder).IsNull();
        await Assert.That(options.PropertyNamingPolicy).IsEqualTo(JsonNamingPolicy.CamelCase);
    }

    /// <summary>Verifies that ISO date JSON object values are inferred as <see cref="DateTime"/>.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [SuppressMessage(
        "Major Code Smell",
        "S6566:Prefer using \"DateTimeOffset\" instead of \"DateTime\"",
        Justification = "The serializer under test infers and returns a DateTime; the expected value must match that type.")]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_InferDateObjectValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>(
            """{"value":"2024-01-02T03:04:05Z"}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(await Assert.That(result!.Value).IsTypeOf<DateTime>())
            .IsEqualTo(new(ExpectedYear, 1, ExpectedDay, ExpectedHour, ExpectedMinute, ExpectedSecond, DateTimeKind.Utc));
    }

    /// <summary>Verifies that string JSON object values are inferred as <see cref="string"/>.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_InferStringObjectValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>(
            """{"value":"Road Runner"}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(await Assert.That(result!.Value).IsTypeOf<string>()).IsEqualTo(RoadRunnerName);
    }

    /// <summary>Verifies that nested JSON object values are deserialized as <see cref="JsonElement"/>.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_DeserializeObjectValuesAsJsonElements()
    {
        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>(
            """{"value":{"company":"ACME"}}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That((await Assert.That(result!.Value).IsTypeOf<JsonElement>()).ValueKind)
            .IsEqualTo(JsonValueKind.Object);
    }

    /// <summary>Verifies that enum object values are serialized using camelCase.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_SerializeObjectEnumValuesAsCamelCase()
    {
        var json = SystemTextJsonSerializer.Serialize(
            new ObjectValueContainer { Value = CamelCaseEnum.ValueOne },
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(json).IsEqualTo("""{"value":"valueOne"}""");
    }

    /// <summary>Verifies that null object values are serialized as JSON null.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_SerializeNullObjectValuesAsJsonNull()
    {
        var json = SystemTextJsonSerializer.Serialize(
            new ObjectValueContainer { Value = null },
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(json).IsEqualTo("""{"value":null}""");
    }

    /// <summary>Verifies that the configured type info resolver is used when serializing object values.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [SuppressMessage(
        "Performance",
        "PSH1416:Cache the serializer options instead of building them per call",
        Justification = "The options embed a per-test resolver instance, so they cannot be cached in a shared static field.")]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_UseResolverWhenSerializingObjectValues()
    {
        var resolver = new TrackingTypeInfoResolver(ObjectValueContainerJsonSerializerContext.Default);
        var options = new JsonSerializerOptions(
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions())
        {
            TypeInfoResolver = resolver
        };

        var json = SystemTextJsonSerializer.Serialize(
            new ObjectValueContainer { Value = RoadRunnerName },
            options);

        await Assert.That(json).IsEqualTo("""{"value":"Road Runner"}""");
        await Assert.That(resolver.RequestedTypes).Contains(typeof(string));
    }

    /// <summary>Verifies that camelCase enum values are deserialized correctly.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_DeserializeCamelCaseEnumValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
            "\"valueOne\"",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(result).IsEqualTo(CamelCaseEnum.ValueOne);
    }

    /// <summary>Verifies that enum values are deserialized case-insensitively.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_DeserializeCaseInsensitiveEnumValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
            "\"VALUEONE\"",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(result).IsEqualTo(CamelCaseEnum.ValueOne);
    }

    /// <summary>Verifies that already-lowercase enum values are deserialized correctly.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_DeserializeLowercaseEnumValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
            "\"alreadyLowercase\"",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(result).IsEqualTo(CamelCaseEnum.alreadyLowercase);
    }

    /// <summary>Verifies that numeric enum values are deserialized correctly.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_DeserializeNumericEnumValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
            "2",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(result).IsEqualTo(CamelCaseEnum.alreadyLowercase);
    }

    /// <summary>Verifies that unsigned numeric enum values are deserialized correctly.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_DeserializeUnsignedNumericEnumValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<UnsignedCamelCaseEnum>(
            "18446744073709551615",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(result).IsEqualTo(UnsignedCamelCaseEnum.Large);
    }

    /// <summary>Verifies that JSON null deserializes to a null nullable enum.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_DeserializeNullNullableEnumValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum?>(
            "null",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(result).IsNull();
    }

    /// <summary>Verifies that an empty string deserializes to a null nullable enum.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_DeserializeEmptyNullableEnumValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum?>(
            "\"\"",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(result).IsNull();
    }

    /// <summary>Verifies that JSON null throws for a non-nullable enum.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_ThrowForNullNonNullableEnumValues() =>
        await Assert.That(
                static () => SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
                    "null",
                    SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()))
            .ThrowsExactly<JsonException>();

    /// <summary>Verifies that an empty string throws for a non-nullable enum.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_ThrowForEmptyNonNullableEnumValues() =>
        await Assert.That(
                static () => SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
                    "\"\"",
                    SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()))
            .ThrowsExactly<JsonException>();

    /// <summary>Verifies that an unknown enum name throws.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_ThrowForInvalidEnumValues() =>
        await Assert.That(
                static () => SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
                    "\"notAValue\"",
                    SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()))
            .ThrowsExactly<JsonException>();

    /// <summary>Verifies that unexpected JSON tokens throw when parsing enums.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_ThrowForUnexpectedTokensWhenParsingEnums() =>
        await Assert.That(
                static () => SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
                    "true",
                    SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()))
            .ThrowsExactly<JsonException>();

    /// <summary>Verifies that undefined enum values are serialized as numbers.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_SerializeUndefinedEnumValuesAsNumbers()
    {
        var json = SystemTextJsonSerializer.Serialize(
            (CamelCaseEnum)UndefinedEnumValue,
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(json).IsEqualTo("999");
    }

    /// <summary>Verifies that undefined unsigned enum values are serialized as unsigned numbers.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_SerializeUndefinedUnsignedEnumValuesAsNumbers()
    {
        var json = SystemTextJsonSerializer.Serialize(
            (UnsignedCamelCaseEnum)UndefinedUnsignedEnumValue,
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(json).IsEqualTo("9223372036854775808");
    }

    /// <summary>Verifies undefined enum dictionary keys are serialized with numeric property names.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_SerializeUndefinedEnumDictionaryKeysAsNumbers()
    {
        var json = SystemTextJsonSerializer.Serialize(
            new Dictionary<CamelCaseEnum, string>
            {
                [(CamelCaseEnum)UndefinedEnumValue] = "unknown"
            },
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(json).IsEqualTo("""{"999":"unknown"}""");
    }

    /// <summary>Verifies undefined unsigned enum dictionary keys are serialized with unsigned numeric property names.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_SerializeUndefinedUnsignedEnumDictionaryKeysAsNumbers()
    {
        var json = SystemTextJsonSerializer.Serialize(
            new Dictionary<UnsignedCamelCaseEnum, string>
            {
                [(UnsignedCamelCaseEnum)UndefinedUnsignedEnumValue] = "unknown"
            },
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(json).IsEqualTo("""{"9223372036854775808":"unknown"}""");
    }

    /// <summary>Verifies that lowercase enum names are serialized unchanged.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_SerializeLowercaseEnumNamesUnchanged()
    {
        var json = SystemTextJsonSerializer.Serialize(
            CamelCaseEnum.alreadyLowercase,
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(json).IsEqualTo("\"alreadyLowercase\"");
    }

    /// <summary>Verifies that enum values are deserialized across a variety of casings.</summary>
    /// <param name="jsonValue">The JSON string value to deserialize.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [Arguments("vAlUeOnE")]
    [Arguments("ValueOne")]
    [Arguments("VALUEONE")]
    [Arguments("valueone")]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_DeserializesEnumValuesWithVariousCasings(
        string jsonValue)
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
            $"\"{jsonValue}\"",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(result).IsEqualTo(CamelCaseEnum.ValueOne);
    }

    /// <summary>Verifies that an exact-case match takes priority over case-insensitive matching when members differ only by case.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_ExactCaseMatchTakesPriorityOverCaseInsensitiveWhenMembersDifferByCase()
    {
        // When enum has members whose names differ only by case, the exact serialized form
        // (camelCase) should be used first (case-sensitive), falling back to case-insensitive only
        // for inputs that do not exactly match any known serialized form.
        // CaseDifferentMembers.Alpha serializes to "alpha" (camelCase),
        // CaseDifferentMembers.ALPHA serializes to "aLPHA" (camelCase).
        // Exact-match lookups must correctly disambiguate these.
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();

        await Assert.That(
            SystemTextJsonSerializer.Deserialize<CaseDifferentMembers>("\"alpha\"", options))
            .IsEqualTo(CaseDifferentMembers.Alpha);
        await Assert.That(
            SystemTextJsonSerializer.Deserialize<CaseDifferentMembers>("\"aLPHA\"", options))
            .IsEqualTo(CaseDifferentMembers.ALPHA);

        // Field names are also accepted via exact match.
        await Assert.That(
            SystemTextJsonSerializer.Deserialize<CaseDifferentMembers>("\"Alpha\"", options))
            .IsEqualTo(CaseDifferentMembers.Alpha);
        await Assert.That(
            SystemTextJsonSerializer.Deserialize<CaseDifferentMembers>("\"ALPHA\"", options))
            .IsEqualTo(CaseDifferentMembers.ALPHA);
    }

    /// <summary>Verifies that source-generated metadata is used when provided to the serializer.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [SuppressMessage(
        "Performance",
        "PSH1416:Cache the serializer options instead of building them per call",
        Justification = "The options embed a per-test tracking resolver instance, so they cannot be cached in a shared static field.")]
    public async Task SystemTextJsonContentSerializer_UsesSourceGeneratedMetadataWhenProvided()
    {
        var resolver = new TrackingTypeInfoResolver(SerializedContentJsonSerializerContext.Default);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = resolver
        };
        var serializer = new SystemTextJsonContentSerializer(options);
        var model = new User
        {
            Name = RoadRunnerName,
            Company = "ACME",
            CreatedAt = "1949-09-17"
        };

        var content = serializer.ToHttpContent(model);
        var roundTrip = await serializer.FromHttpContentAsync<User>(content);

        await Assert.That(roundTrip).IsNotNull();
        await Assert.That(roundTrip!.Name).IsEqualTo(model.Name);
        await Assert.That(roundTrip.Company).IsEqualTo(model.Company);
        await Assert.That(roundTrip.CreatedAt).IsEqualTo(model.CreatedAt);
        await Assert.That(resolver.RequestedTypes).Contains(typeof(User));
    }

    /// <summary>Verifies the synchronous DeserializeFromString uses source-generated metadata when provided (#1591).</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [SuppressMessage(
        "Performance",
        "PSH1416:Cache the serializer options instead of building them per call",
        Justification = "The options embed a per-test tracking resolver instance, so they cannot be cached in a shared static field.")]
    public async Task SystemTextJsonContentSerializer_DeserializeFromString_UsesSourceGeneratedMetadata()
    {
        var resolver = new TrackingTypeInfoResolver(SerializedContentJsonSerializerContext.Default);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = resolver
        };
        var serializer = new SystemTextJsonContentSerializer(options);

        var roundTrip = serializer.DeserializeFromString<User>("{\"name\":\"Road Runner\"}");

        await Assert.That(roundTrip).IsNotNull();
        await Assert.That(roundTrip!.Name).IsEqualTo(RoadRunnerName);
        await Assert.That(resolver.RequestedTypes).Contains(typeof(User));
    }

    /// <summary>Verifies the synchronous DeserializeFromString uses reflection metadata by default (#1591).</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DeserializeFromString_UsesReflectionByDefault()
    {
        var serializer = new SystemTextJsonContentSerializer();

        var roundTrip = serializer.DeserializeFromString<User>("{\"name\":\"Road Runner\"}");

        await Assert.That(roundTrip).IsNotNull();
        await Assert.That(roundTrip!.Name).IsEqualTo(RoadRunnerName);
    }

    /// <summary>Verifies that enum dictionary keys round-trip through the serializer.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_RoundTripsEnumDictionaryKeys()
    {
        var serializer = new SystemTextJsonContentSerializer();

        var source = new Dictionary<CamelCaseEnum, string>
        {
            [CamelCaseEnum.ValueOne] = FirstEnumValue,
            [CamelCaseEnum.alreadyLowercase] = "second"
        };

        var content = serializer.ToHttpContent(source);
        var serialized = await content.ReadAsStringAsync();

        var roundTrip = await serializer.FromHttpContentAsync<Dictionary<CamelCaseEnum, string>>(
            new StringContent(serialized, Encoding.UTF8, JsonMediaType));

        await Assert.That(roundTrip).IsNotNull();
        await Assert.That(roundTrip![CamelCaseEnum.ValueOne]).IsEqualTo(FirstEnumValue);
        await Assert.That(roundTrip[CamelCaseEnum.alreadyLowercase]).IsEqualTo("second");
    }

    /// <summary>Verifies nullable enum dictionary key conversion handles empty and non-empty property names.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_NullableEnumPropertyNamesRoundTripThroughConverter()
    {
        var (emptyNameValue, namedValue, json) = ReadAndWriteNullableEnumPropertyNames();

        await Assert.That(emptyNameValue).IsNull();
        await Assert.That(namedValue).IsEqualTo(CamelCaseEnum.ValueOne);
        await Assert.That(json).IsEqualTo("""{"":"empty","valueOne":"first"}""");
    }

    /// <summary>Verifies nullable enum values write null and concrete enum names through the custom converter.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_NullableEnumValuesWriteThroughConverter()
    {
        var json = WriteNullableEnumValues();

        await Assert.That(json).IsEqualTo("""[null,"valueOne"]""");
    }

#if NET9_0_OR_GREATER
    /// <summary>Verifies that JsonStringEnumMemberName is honored when serializing and deserializing.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_SupportsJsonStringEnumMemberName()
    {
        var serializer = new SystemTextJsonContentSerializer(
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        var content = serializer.ToHttpContent(
            new EnumMemberNameEnvelope { Status = EnumMemberNameStatus.TotallyReady });
        var serialized = await content.ReadAsStringAsync();
        var roundTrip = await serializer.FromHttpContentAsync<EnumMemberNameEnvelope>(
            new StringContent("{\"status\":\"totally-ready\"}", Encoding.UTF8, JsonMediaType));

        await Assert.That(serialized).Contains("totally-ready");
        await Assert.That(roundTrip).IsNotNull();
        await Assert.That(roundTrip!.Status).IsEqualTo(EnumMemberNameStatus.TotallyReady);
    }

    /// <summary>Verifies that RestService uses the default enum converter with JsonStringEnumMemberName.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task RestService_UsesDefaultEnumConverterWithJsonStringEnumMemberName()
    {
        var settings = new RefitSettings(
            new SystemTextJsonContentSerializer(
                SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()))
        {
            HttpMessageHandlerFactory = static () => new StubHttpMessageHandler(static _ =>
                Task.FromResult(
                    new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\"status\":\"totally-ready\"}",
                            Encoding.UTF8,
                            JsonMediaType)
                    }))
        };

        var api = RestService.For<IIssue2067StatusApi>(BaseAddress, settings);
        var result = await api.GetStatusAsync();

        await Assert.That(result.Status).IsEqualTo(EnumMemberNameStatus.TotallyReady);
    }

    /// <summary>Verifies that the default serializer honors JsonStringEnumMemberName with an attributed converter.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task RestService_DefaultSystemTextJsonSerializerHonorsJsonStringEnumMemberNameWithAttributedConverter()
    {
        var serializedBody = string.Empty;
        var settings = new RefitSettings(new SystemTextJsonContentSerializer())
        {
            HttpMessageHandlerFactory = () => new StubHttpMessageHandler(async request =>
            {
                serializedBody = await request.Content!.ReadAsStringAsync();
                return new(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, JsonMediaType)
                };
            })
        };

        var api = RestService.For<IIssue2083ColorApi>(BaseAddress, settings);
        await api.PostColorAsync(new() { Color = EnumMemberNameColor.Green });

        await Assert.That(serializedBody).IsEqualTo("""{"color":"GREEN"}""");
    }
#endif

    /// <summary>Exercises the nullable enum converter's property-name read and write paths.</summary>
    /// <returns>The values read from property names and the JSON written through the converter.</returns>
    private static (CamelCaseEnum? EmptyNameValue, CamelCaseEnum? NamedValue, string Json) ReadAndWriteNullableEnumPropertyNames()
    {
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
        var converter = (JsonConverter<CamelCaseEnum?>)options.GetConverter(
            typeof(CamelCaseEnum?));
        var reader = new Utf8JsonReader("""{"":"empty","valueOne":"first"}"""u8);
        _ = reader.Read();
        _ = reader.Read();
        var emptyNameValue = converter.ReadAsPropertyName(ref reader, typeof(CamelCaseEnum?), options);
        _ = reader.Read();
        _ = reader.Read();
        var namedValue = converter.ReadAsPropertyName(ref reader, typeof(CamelCaseEnum?), options);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            // The nullable converter intentionally supports null keys by writing an empty property name.
            converter.WriteAsPropertyName(writer, null!, options);
            writer.WriteStringValue("empty");
            converter.WriteAsPropertyName(writer, CamelCaseEnum.ValueOne, options);
            writer.WriteStringValue(FirstEnumValue);
            writer.WriteEndObject();
        }

        return (emptyNameValue, namedValue, Encoding.UTF8.GetString(stream.ToArray()));
    }

    /// <summary>Writes nullable enum values through the converter directly.</summary>
    /// <returns>The JSON written by the converter.</returns>
    private static string WriteNullableEnumValues()
    {
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
        var converter = (JsonConverter<CamelCaseEnum?>)options.GetConverter(
            typeof(CamelCaseEnum?));

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartArray();
            converter.Write(writer, null, options);
            converter.Write(writer, CamelCaseEnum.ValueOne, options);
            writer.WriteEndArray();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
