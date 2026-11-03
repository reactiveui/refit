// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SystemTextJsonSerializer = System.Text.Json.JsonSerializer;

namespace Refit.Tests;

/// <summary>Verifies how the System.Text.Json content serializer converts JSON values and enums with the fast-path-eligible default options.</summary>
public partial class SerializedContentTests
{
    /// <summary>Verifies the default options carry nothing that disables the System.Text.Json fast-path.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_AreFastPathEligible()
    {
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();

        await Assert.That(options.Converters).IsEmpty();
        await Assert.That(options.NumberHandling).IsEqualTo(JsonNumberHandling.Strict);
        await Assert.That(options.ReferenceHandler).IsNull();
        await Assert.That(options.Encoder).IsNull();
        await Assert.That(options.DictionaryKeyPolicy).IsNull();
        await Assert.That(options.PropertyNamingPolicy).IsEqualTo(JsonNamingPolicy.CamelCase);
    }

    /// <summary>Verifies the fast-path options carry nothing that disables the System.Text.Json fast-path.</summary>
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

    /// <summary>Verifies the default options reject numbers written as JSON strings.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_RejectNumbersFromString() =>
        await Assert
            .That(static () => SystemTextJsonSerializer.Deserialize<NumberContainer>(
                """{"id":"123","amount":"9.99"}""",
                SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()))
            .ThrowsExactly<JsonException>();

    /// <summary>Verifies the default options read JSON numbers.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_ReadNumbers()
    {
        var result = SystemTextJsonSerializer.Deserialize<NumberContainer>(
            """{"id":123,"amount":9.99}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(result!.Id).IsEqualTo(ExpectedId);
        await Assert.That(result.Amount).IsEqualTo(ExpectedAmount);
    }

    /// <summary>Verifies the default options write numbers as JSON numbers, not strings.</summary>
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

    /// <summary>Verifies that object JSON values are deserialized as <see cref="JsonElement"/>.</summary>
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

    /// <summary>Verifies that primitive object JSON values are deserialized as <see cref="JsonElement"/>, not inferred CLR types.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_DeserializePrimitiveObjectValuesAsJsonElements()
    {
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();

        var text = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>("""{"value":"Road Runner"}""", options);
        var number = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>("""{"value":42}""", options);
        var flag = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>("""{"value":true}""", options);

        await Assert.That((await Assert.That(text!.Value).IsTypeOf<JsonElement>()).GetString()).IsEqualTo(RoadRunnerName);
        await Assert.That((await Assert.That(number!.Value).IsTypeOf<JsonElement>()).GetInt64()).IsEqualTo(ExpectedIntegralValue);
        await Assert.That((await Assert.That(flag!.Value).IsTypeOf<JsonElement>()).GetBoolean()).IsTrue();
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1869",
        Justification = "Each test needs a fresh tracking resolver and uncached contracts to observe metadata lookups; shared cached options can bypass that resolver and invalidate the assertion.")]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_UseResolverWhenSerializingObjectValues()
    {
        var resolver = new TrackingTypeInfoResolver(ObjectValueContainerJsonSerializerContext.Default);
        var options = new JsonSerializerOptions(
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()) { TypeInfoResolver = resolver, };

        var json = SystemTextJsonSerializer.Serialize(
            new ObjectValueContainer { Value = RoadRunnerName },
            options);

        await Assert.That(json).IsEqualTo("""{"value":"Road Runner"}""");
        await Assert.That(resolver.RequestedTypes).Contains(typeof(string));
    }

    /// <summary>Verifies that enums are serialized as numbers by the default options.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_SerializeEnumValuesAsNumbers()
    {
        var json = SystemTextJsonSerializer.Serialize(
            CamelCaseEnum.AlreadyLowercase,
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(json).IsEqualTo("2");
    }

    /// <summary>Verifies that numeric enum values are deserialized correctly.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_DeserializeNumericEnumValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
            "2",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(result).IsEqualTo(CamelCaseEnum.AlreadyLowercase);
    }

    /// <summary>Verifies that string enum names are rejected unless a source-generated context enables them.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_RejectEnumNames() =>
        await Assert
            .That(static () => SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
                "\"valueOne\"",
                SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()))
            .ThrowsExactly<JsonException>();

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

    /// <summary>Verifies that a numeric nullable enum token deserializes to its value.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_DeserializeNumericNullableEnumValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum?>(
            "2",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(result).IsEqualTo(CamelCaseEnum.AlreadyLowercase);
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
            new Dictionary<CamelCaseEnum, string> { [(CamelCaseEnum)UndefinedEnumValue] = "unknown", },
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(json).IsEqualTo("""{"999":"unknown"}""");
    }

    /// <summary>Verifies undefined unsigned enum dictionary keys are serialized with unsigned numeric property names.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_SerializeUndefinedUnsignedEnumDictionaryKeysAsNumbers()
    {
        var json = SystemTextJsonSerializer.Serialize(
            new Dictionary<UnsignedCamelCaseEnum, string> { [(UnsignedCamelCaseEnum)UndefinedUnsignedEnumValue] = "unknown", },
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(json).IsEqualTo("""{"9223372036854775808":"unknown"}""");
    }

    /// <summary>Verifies that a source-generated context with string enums round-trips through the serializer.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_StringEnumContext_RoundTripsEnumNames()
    {
        var serializer = new SystemTextJsonContentSerializer(StringEnumOptions);

        var content = serializer.ToHttpContent(new StringEnumEnvelope { Value = CamelCaseEnum.ValueOne });
        var serialized = await content.ReadAsStringAsync();
        var roundTrip = await serializer.FromHttpContentAsync<StringEnumEnvelope>(
            new StringContent(serialized, Encoding.UTF8, JsonMediaType));

        await Assert.That(serialized).Contains("\"ValueOne\"");
        await Assert.That(roundTrip!.Value).IsEqualTo(CamelCaseEnum.ValueOne);
    }

    /// <summary>Verifies the opt-in reflection converters restore camelCase enum names.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AddReflectionConverters_RoundTripsCamelCaseEnumNames()
    {
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions().AddReflectionConverters();

        var json = SystemTextJsonSerializer.Serialize(CamelCaseEnum.ValueOne, options);
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum>("\"VALUEONE\"", options);

        await Assert.That(json).IsEqualTo("\"valueOne\"");
        await Assert.That(result).IsEqualTo(CamelCaseEnum.ValueOne);
    }

    /// <summary>Verifies the opt-in reflection converters infer CLR primitives for object values.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AddReflectionConverters_InfersObjectValues()
    {
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions().AddReflectionConverters();

        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>("""{"value":42}""", options);

        await Assert.That(await Assert.That(result!.Value).IsTypeOf<long>()).IsEqualTo(ExpectedIntegralValue);
    }

    /// <summary>Verifies that source-generated metadata is used when provided to the serializer.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_UsesSourceGeneratedMetadataWhenProvided()
    {
        var resolver = new TrackingTypeInfoResolver(SerializedContentJsonSerializerContext.Default);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = resolver, };
        var serializer = new SystemTextJsonContentSerializer(options);
        var model = new User { Name = RoadRunnerName, Company = "ACME", CreatedAt = "1949-09-17", };

        var content = serializer.ToHttpContent(model);
        var roundTrip = await serializer.FromHttpContentAsync<User>(content);

        await Assert.That(roundTrip).IsNotNull();
        await Assert.That(roundTrip!.Name).IsEqualTo(model.Name);
        await Assert.That(roundTrip.Company).IsEqualTo(model.Company);
        await Assert.That(roundTrip.CreatedAt).IsEqualTo(model.CreatedAt);
        await Assert.That(resolver.RequestedTypes).Contains(typeof(User));
    }

    /// <summary>Verifies the synchronous DeserializeFromString uses source-generated metadata when provided.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DeserializeFromString_UsesSourceGeneratedMetadata()
    {
        var resolver = new TrackingTypeInfoResolver(SerializedContentJsonSerializerContext.Default);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = resolver, };
        var serializer = new SystemTextJsonContentSerializer(options);

        var roundTrip = serializer.DeserializeFromString<User>("{\"name\":\"Road Runner\"}");

        await Assert.That(roundTrip).IsNotNull();
        await Assert.That(roundTrip!.Name).IsEqualTo(RoadRunnerName);
        await Assert.That(resolver.RequestedTypes).Contains(typeof(User));
    }

    /// <summary>Verifies the synchronous DeserializeFromString uses reflection metadata by default.</summary>
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

        var source = new Dictionary<CamelCaseEnum, string> { [CamelCaseEnum.ValueOne] = FirstEnumValue, [CamelCaseEnum.AlreadyLowercase] = "second", };

        var content = serializer.ToHttpContent(source);
        var serialized = await content.ReadAsStringAsync();

        var roundTrip = await serializer.FromHttpContentAsync<Dictionary<CamelCaseEnum, string>>(
            new StringContent(serialized, Encoding.UTF8, JsonMediaType));

        await Assert.That(roundTrip).IsNotNull();
        await Assert.That(roundTrip![CamelCaseEnum.ValueOne]).IsEqualTo(FirstEnumValue);
        await Assert.That(roundTrip[CamelCaseEnum.AlreadyLowercase]).IsEqualTo("second");
    }

#if NET9_0_OR_GREATER
    /// <summary>Verifies that JsonStringEnumMemberName is honored through a source-generated string-enum context.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_SupportsJsonStringEnumMemberName()
    {
        var serializer = new SystemTextJsonContentSerializer(StringEnumOptions);

        var content = serializer.ToHttpContent(
            new EnumMemberNameEnvelope { Status = EnumMemberNameStatus.TotallyReady });
        var serialized = await content.ReadAsStringAsync();
        var roundTrip = await serializer.FromHttpContentAsync<EnumMemberNameEnvelope>(
            new StringContent("{\"status\":\"totally-ready\"}", Encoding.UTF8, JsonMediaType));

        await Assert.That(serialized).Contains("totally-ready");
        await Assert.That(roundTrip).IsNotNull();
        await Assert.That(roundTrip!.Status).IsEqualTo(EnumMemberNameStatus.TotallyReady);
    }

    /// <summary>Verifies that RestService honors JsonStringEnumMemberName when a string-enum context is supplied.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task RestService_HonorsJsonStringEnumMemberNameWithStringEnumContext()
    {
        var settings = new RefitSettings(new SystemTextJsonContentSerializer(StringEnumOptions))
        {
            HttpMessageHandlerFactory = static () => new StubHttpMessageHandler(static _ =>
                Task.FromResult(
                    new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"totally-ready\"}", Encoding.UTF8, JsonMediaType), })),
        };

        var api = RestService.For<IEnumMemberNameStatusApi>(BaseAddress, settings);
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
                return new(System.Net.HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, JsonMediaType), };
            }),
        };

        var api = RestService.For<IEnumMemberNameColorApi>(BaseAddress, settings);
        await api.PostColorAsync(new() { Color = EnumMemberNameColor.Green });

        await Assert.That(serializedBody).IsEqualTo("""{"color":"GREEN"}""");
    }
#endif

    /// <summary>Verifies a custom serializer that implements the reflection provider names fields itself.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SerializerFieldNames_UsesCustomProviderWhenImplemented()
    {
        var property = typeof(SystemTextFieldNameModel).GetProperty(nameof(SystemTextFieldNameModel.Name));

        var fieldName = SerializerFieldNames.Resolve(new ProviderTestSerializer(), property!);

        await Assert.That(fieldName).IsEqualTo("from-provider");
    }

    /// <summary>Verifies the Newtonsoft attribute is read by its type name, so no Newtonsoft reference is needed.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SerializerFieldNames_ReadsNewtonsoftNameByTypeName()
    {
        var named = typeof(NewtonsoftNamedModel).GetProperty(nameof(NewtonsoftNamedModel.Named));
        var plain = typeof(NewtonsoftNamedModel).GetProperty(nameof(NewtonsoftNamedModel.Plain));

        await Assert.That(SerializerFieldNames.ResolveForStyle(ContentSerializerNameStyle.Newtonsoft, named!)).IsEqualTo("newtonsoft_name");
        await Assert.That(SerializerFieldNames.ResolveForStyle(ContentSerializerNameStyle.Newtonsoft, plain!)).IsNull();
    }

    /// <summary>Model whose property carries the stand-in Newtonsoft attribute.</summary>
    internal sealed class NewtonsoftNamedModel
    {
        /// <summary>Gets or sets the property named through the stand-in attribute.</summary>
        [global::Newtonsoft.Json.JsonProperty("newtonsoft_name")]
        public string? Named { get; set; }

        /// <summary>Gets or sets a property with no attribute.</summary>
        public string? Plain { get; set; }
    }

    /// <summary>Serializer that reports no name style and offers no provider.</summary>
    internal sealed class ContentOnlyTestSerializer : IHttpContentSerializer
    {
        /// <inheritdoc/>
        public HttpContent ToHttpContent<T>(T item) => new StringContent(string.Empty);

        /// <inheritdoc/>
        public Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default) =>
            Task.FromResult<T?>(default);
    }

    /// <summary>Serializer that names every field itself through the reflection provider.</summary>
    internal sealed class ProviderTestSerializer : IHttpContentSerializer, IReflectionFieldNameProvider
    {
        /// <inheritdoc/>
        public HttpContent ToHttpContent<T>(T item) => new StringContent(string.Empty);

        /// <inheritdoc/>
        public Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default) =>
            Task.FromResult<T?>(default);

        /// <inheritdoc/>
        public string? GetFieldNameForProperty(System.Reflection.PropertyInfo propertyInfo) => "from-provider";
    }

    /// <summary>Container for an enum property used to verify string-enum source generation.</summary>
    internal sealed class StringEnumEnvelope
    {
        /// <summary>Gets or sets the enum value.</summary>
        public CamelCaseEnum Value { get; set; }
    }

    /// <summary>Source-generated context that writes enums as strings.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true)]
    [JsonSerializable(typeof(StringEnumEnvelope))]
#if NET9_0_OR_GREATER
    [JsonSerializable(typeof(EnumMemberNameEnvelope))]
#endif
    internal sealed partial class StringEnumJsonSerializerContext : JsonSerializerContext;
}
