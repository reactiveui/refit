// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SystemTextJsonSerializer = System.Text.Json.JsonSerializer;

namespace Refit.Tests;

/// <summary>Tests that verify how Refit serializes request bodies and deserializes responses across content serializers.</summary>
public partial class SerializedContentTests
{
    /// <summary>The base address used when creating Refit clients for these tests.</summary>
    private const string BaseAddress = "https://api/";

#if NET9_0_OR_GREATER
    /// <summary>Status enum used to verify JsonStringEnumMemberName handling.</summary>
    public enum EnumMemberNameStatus
    {
        /// <summary>The ready status, serialized as "totally-ready".</summary>
        [JsonStringEnumMemberName("totally-ready")]
        TotallyReady,

        /// <summary>The needs-review status.</summary>
        NeedsReview
    }

    /// <summary>Color enum used to verify JsonStringEnumMemberName handling with an attributed converter.</summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter<EnumMemberNameColor>))]
    public enum EnumMemberNameColor
    {
        /// <summary>The green color, serialized as "GREEN".</summary>
        [JsonStringEnumMemberName("GREEN")]
        Green,

        /// <summary>The red color, serialized as "RED".</summary>
        [JsonStringEnumMemberName("RED")]
        Red
    }
#endif

    /// <summary>Enum whose members exercise camelCase serialization and case-insensitive deserialization.</summary>
    private enum CamelCaseEnum
    {
        /// <summary>The first value, serialized as "valueOne".</summary>
        ValueOne = 1,

        /// <summary>An already-lowercase value, serialized unchanged as "alreadyLowercase".</summary>
        alreadyLowercase = 2
    }

    /// <summary>Enum with an unsigned backing type used to verify large numeric values.</summary>
    private enum UnsignedCamelCaseEnum : ulong
    {
        /// <summary>The first value, serialized as "small".</summary>
        Small = 1,

        /// <summary>The maximum unsigned value, serialized as "large".</summary>
        Large = ulong.MaxValue
    }

    /// <summary>
    /// Members Alpha and ALPHA differ only by case; this enum is used to verify that
    /// the case-sensitive lookup takes priority and the correct member is chosen.
    /// </summary>
    private enum CaseDifferentMembers
    {
        /// <summary>The member serialized as "alpha" (camelCase).</summary>
        Alpha = 1,

        /// <summary>The member serialized as "aLPHA" (camelCase), differing from <see cref="Alpha"/> only by case.</summary>
        ALPHA = 2,
    }

    /// <summary>Marker request type used to verify serialization when the declared type is an interface.</summary>
    [SuppressMessage(
        "Design",
        "CA1040:Avoid empty interfaces",
        Justification = "Intentional empty fixture interface used to verify Refit serialization when the declared type is an interface.")]
    [SuppressMessage(
        "Design",
        "SST1437:Add members to type or remove it",
        Justification = "Intentional empty fixture interface used to verify Refit serialization when the declared type is an interface.")]
    public interface InterfaceCreateWeaponRequest;

    /// <summary>Refit API used to verify polymorphic base-type body serialization.</summary>
    public interface IPolymorphicRequestApi
    {
        /// <summary>Sends a weapon creation request.</summary>
        /// <param name="request">The weapon request to serialize.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Post("/weapons")]
        Task CreateWeapon(CreateWeaponRequest request);
    }

    /// <summary>Refit API used to verify body serialization when the declared type is an interface.</summary>
    public interface IInterfaceRequestApi
    {
        /// <summary>Sends a weapon creation request.</summary>
        /// <param name="request">The weapon request to serialize.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Post("/weapons")]
        Task CreateWeapon([Body] InterfaceCreateWeaponRequest request);
    }

    /// <summary>Refit API used to verify body serialization when the declared type is abstract.</summary>
    public interface IAbstractRequestApi
    {
        /// <summary>Sends a weapon creation request.</summary>
        /// <param name="request">The weapon request to serialize.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Post("/weapons")]
        Task CreateWeapon([Body] AbstractCreateWeaponRequest request);
    }

    /// <summary>Refit API used to verify resolver-provided polymorphic body metadata.</summary>
    public interface IResolverPolymorphicRequestApi
    {
        /// <summary>Sends a weapon creation request.</summary>
        /// <param name="request">The weapon request to serialize.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Post("/weapons")]
        Task CreateWeapon([Body] ResolverPolymorphicRequest request);
    }

#if NET9_0_OR_GREATER
    /// <summary>Refit API used to verify JsonStringEnumMemberName handling on responses.</summary>
    public interface IIssue2067StatusApi
    {
        /// <summary>Gets the current status.</summary>
        /// <returns>A task that resolves to the status envelope.</returns>
        [Get("/status")]
        Task<EnumMemberNameEnvelope> GetStatusAsync();
    }

    /// <summary>Refit API used to verify JsonStringEnumMemberName handling on requests.</summary>
    public interface IIssue2083ColorApi
    {
        /// <summary>Posts a color envelope.</summary>
        /// <param name="body">The color envelope to serialize.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Post("/color")]
        Task PostColorAsync([Body] EnumMemberNameColorEnvelope body);
    }
#endif

    /// <summary>Verifies that a request requiring a serialized body completes without deadlocking.</summary>
    /// <param name="contentSerializerType">The content serializer implementation under test.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [Arguments(typeof(NewtonsoftJsonContentSerializer))]
    [Arguments(typeof(SystemTextJsonContentSerializer))]
    [Arguments(typeof(XmlContentSerializer))]
    public async Task WhenARequestRequiresABodyThenItDoesNotDeadlock(Type contentSerializerType)
    {
        if (
            Activator.CreateInstance(contentSerializerType) is not IHttpContentSerializer serializer)
        {
            throw new ArgumentException(
                $"{contentSerializerType.FullName} does not implement {nameof(IHttpContentSerializer)}");
        }

        var handler = new MockPushStreamContentHttpMessageHandler
        {
            Asserts = async content =>
                new StringContent(await content.ReadAsStringAsync().ConfigureAwait(false))
        };

        var settings = new RefitSettings(serializer) { HttpMessageHandlerFactory = () => handler };

        var fixture = RestService.For<IGitHubApi>(BaseAddress, settings);

        var fixtureTask = await RunTaskWithATimeLimit(fixture.CreateUser(new()))
            .ConfigureAwait(false);
        await Assert.That(fixtureTask.IsCompleted).IsTrue();
        await Assert.That(fixtureTask.Status).IsEqualTo(TaskStatus.RanToCompletion);
    }

    /// <summary>Verifies that a request body is serialized and round-trips back to the original model.</summary>
    /// <param name="contentSerializerType">The content serializer implementation under test.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [Arguments(typeof(NewtonsoftJsonContentSerializer))]
    [Arguments(typeof(SystemTextJsonContentSerializer))]
    [Arguments(typeof(XmlContentSerializer))]
    public async Task WhenARequestRequiresABodyThenItIsSerialized(Type contentSerializerType)
    {
        if (
            Activator.CreateInstance(contentSerializerType) is not IHttpContentSerializer serializer)
        {
            throw new ArgumentException(
                $"{contentSerializerType.FullName} does not implement {nameof(IHttpContentSerializer)}");
        }

        var model = new User
        {
            Name = "Wile E. Coyote",
            CreatedAt = new DateOnly(1949, 9, 16).ToString(),
            Company = "ACME",
        };

        var handler = new MockPushStreamContentHttpMessageHandler
        {
            Asserts = async content =>
            {
                var stringContent = new StringContent(
                    await content.ReadAsStringAsync().ConfigureAwait(false));
                var user = await serializer
                    .FromHttpContentAsync<User>(content)
                    .ConfigureAwait(false);
                await Assert.That(user).IsNotSameReferenceAs(model);
                await Assert.That(user!.Name).IsEqualTo(model.Name);
                await Assert.That(user.CreatedAt).IsEqualTo(model.CreatedAt);
                await Assert.That(user.Company).IsEqualTo(model.Company);

                // Returns some content so that the serializer does not complain.
                return stringContent;
            }
        };

        var settings = new RefitSettings(serializer) { HttpMessageHandlerFactory = () => handler };

        var fixture = RestService.For<IGitHubApi>(BaseAddress, settings);

        var fixtureTask = await RunTaskWithATimeLimit(fixture.CreateUser(model))
            .ConfigureAwait(false);

        await Assert.That(fixtureTask.IsCompleted).IsTrue();
    }

    /// <summary>Verifies the default content serializer selection for <see cref="RefitSettings"/>.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task VerityDefaultSerializer()
    {
        var settings = new RefitSettings();

        await Assert.That(settings.ContentSerializer).IsNotNull();
        await Assert.That(settings.ContentSerializer).IsTypeOf<SystemTextJsonContentSerializer>();

        settings = new(new NewtonsoftJsonContentSerializer());

        await Assert.That(settings.ContentSerializer).IsNotNull();
        await Assert.That(settings.ContentSerializer).IsTypeOf<NewtonsoftJsonContentSerializer>();
    }

    /// <summary>Verifies stream deserialization using the System.Text.Json content serializer.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task StreamDeserialization_UsingSystemTextJsonContentSerializer()
    {
        var model = new TestAliasObject
        {
            ShortNameForAlias = nameof(StreamDeserialization_UsingSystemTextJsonContentSerializer),
            ShortNameForJsonProperty = nameof(TestAliasObject)
        };

        var serializer = new SystemTextJsonContentSerializer();

        var json = serializer.ToHttpContent(model);

        var result = await serializer.FromHttpContentAsync<TestAliasObject>(json);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ShortNameForAlias).IsEqualTo(model.ShortNameForAlias);
        await Assert.That(result.ShortNameForJsonProperty).IsEqualTo(model.ShortNameForJsonProperty);
    }

    /// <summary>Verifies that the System.Text.Json content serializer sets the expected content headers.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task StreamDeserialization_UsingSystemTextJsonContentSerializer_SetsCorrectHeaders()
    {
        var model = new TestAliasObject
        {
            ShortNameForAlias = nameof(StreamDeserialization_UsingSystemTextJsonContentSerializer),
            ShortNameForJsonProperty = nameof(TestAliasObject)
        };

        var serializer = new SystemTextJsonContentSerializer();

        var json = serializer.ToHttpContent(model);

        await Assert.That(json.Headers.ContentType).IsNotNull();
        await Assert.That(json.Headers.ContentType!.CharSet).IsEqualTo("utf-8");
        await Assert.That(json.Headers.ContentType.MediaType).IsEqualTo("application/json");
    }

    /// <summary>Verifies that the Newtonsoft content serializer never falls back to synchronous stream reads.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task StreamDeserialization_UsingNewtonsoftJsonContentSerializer_DoesNotUseSynchronousReads()
    {
        var serializer = new NewtonsoftJsonContentSerializer();
        var content = new AsyncOnlyJsonContent("{\"name\":\"Road Runner\"}");

        var result = await serializer.FromHttpContentAsync<User>(content);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Road Runner");
    }

    /// <summary>Verifies that the Newtonsoft content serializer returns the default value for null content.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task StreamDeserialization_UsingNewtonsoftJsonContentSerializer_ReturnsDefaultForNullContent()
    {
        var serializer = new NewtonsoftJsonContentSerializer();

        var result = await serializer.FromHttpContentAsync<User>(null!);

        await Assert.That(result).IsNull();
    }

    /// <summary>Verifies that the Newtonsoft content serializer returns the JsonProperty name for a property.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NewtonsoftJsonContentSerializer_GetFieldNameForProperty_ReturnsJsonPropertyName()
    {
        var serializer = new NewtonsoftJsonContentSerializer();
        var property = typeof(NewtonsoftFieldNameModel).GetProperty(
            nameof(NewtonsoftFieldNameModel.Name));

        var fieldName = serializer.GetFieldNameForProperty(property!);

        await Assert.That(fieldName).IsEqualTo("json_name");
    }

    /// <summary>Verifies that the Newtonsoft content serializer returns null when no JsonProperty attribute exists.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NewtonsoftJsonContentSerializer_GetFieldNameForProperty_ReturnsNullWithoutJsonPropertyAttribute()
    {
        var serializer = new NewtonsoftJsonContentSerializer();
        var property = typeof(NewtonsoftFieldNameModel).GetProperty(
            nameof(NewtonsoftFieldNameModel.Unaliased));

        var fieldName = serializer.GetFieldNameForProperty(property!);

        await Assert.That(fieldName).IsNull();
    }

    /// <summary>Verifies that the Newtonsoft content serializer throws when the property argument is null.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NewtonsoftJsonContentSerializer_GetFieldNameForProperty_ThrowsForNullProperty()
    {
        var serializer = new NewtonsoftJsonContentSerializer();

        var exception = await Assert.That(() => serializer.GetFieldNameForProperty(null!)).ThrowsExactly<ArgumentNullException>();

        await Assert.That(exception!.ParamName).IsEqualTo("propertyInfo");
    }

    /// <summary>Verifies quoted charsets are unwrapped before Newtonsoft deserialization resolves the encoding.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NewtonsoftJsonContentSerializer_FromHttpContentAsync_UnwrapsQuotedCharset()
    {
        var serializer = new NewtonsoftJsonContentSerializer();
        var content = new StringContent(
            """{"Name":"Utf16 User"}""",
            Encoding.Unicode,
            "application/json");
        content.Headers.ContentType!.CharSet = "\"utf-16\"";

        var result = await serializer.FromHttpContentAsync<User>(content);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Utf16 User");
    }

    /// <summary>Verifies invalid Newtonsoft response charsets fail with a clear operation exception.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NewtonsoftJsonContentSerializer_FromHttpContentAsync_ThrowsForInvalidCharset()
    {
        var serializer = new NewtonsoftJsonContentSerializer();
        var content = new StringContent("""{"Name":"Invalid"}""", Encoding.UTF8, "application/json");
        content.Headers.ContentType!.CharSet = "not-a-real-charset";

        await Assert.That(() => serializer.FromHttpContentAsync<User>(content))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies that the System.Text.Json content serializer returns the JsonPropertyName for a property.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_GetFieldNameForProperty_ReturnsJsonPropertyName()
    {
        var serializer = new SystemTextJsonContentSerializer();
        var property = typeof(SystemTextFieldNameModel).GetProperty(
            nameof(SystemTextFieldNameModel.Name));

        var fieldName = serializer.GetFieldNameForProperty(property!);

        await Assert.That(fieldName).IsEqualTo("json_name");
    }

    /// <summary>Verifies that the System.Text.Json content serializer returns null without a JsonPropertyName attribute.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_GetFieldNameForProperty_ReturnsNullWithoutJsonPropertyNameAttribute()
    {
        var serializer = new SystemTextJsonContentSerializer();
        var property = typeof(SystemTextFieldNameModel).GetProperty(
            nameof(SystemTextFieldNameModel.Unaliased));

        var fieldName = serializer.GetFieldNameForProperty(property!);

        await Assert.That(fieldName).IsNull();
    }

    /// <summary>Verifies that the System.Text.Json content serializer throws when the property argument is null.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_GetFieldNameForProperty_ThrowsForNullProperty()
    {
        var serializer = new SystemTextJsonContentSerializer();

        var exception = await Assert.That(() => serializer.GetFieldNameForProperty(null!)).ThrowsExactly<ArgumentNullException>();

        await Assert.That(exception!.ParamName).IsEqualTo("propertyInfo");
    }

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
            .ThrowsExactly<System.Text.Json.JsonException>();
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

        await Assert.That(await Assert.That(result!.Value).IsTypeOf<long>()).IsEqualTo(42L);
    }

    /// <summary>Verifies that floating-point JSON object values are inferred as <see cref="double"/>.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_InferFloatingPointObjectValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>(
            """{"value":42.5}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(await Assert.That(result!.Value).IsTypeOf<double>()).IsEqualTo(42.5);
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
            .IsEqualTo(new(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc));
    }

    /// <summary>Verifies that string JSON object values are inferred as <see cref="string"/>.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_InferStringObjectValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>(
            """{"value":"Road Runner"}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(await Assert.That(result!.Value).IsTypeOf<string>()).IsEqualTo("Road Runner");
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
        "CA1869:Cache and reuse 'JsonSerializerOptions' instances",
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
            new ObjectValueContainer { Value = "Road Runner" },
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
                () => SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
                    "null",
                    SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()))
            .ThrowsExactly<System.Text.Json.JsonException>();

    /// <summary>Verifies that an empty string throws for a non-nullable enum.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_ThrowForEmptyNonNullableEnumValues() =>
        await Assert.That(
                () => SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
                    "\"\"",
                    SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()))
            .ThrowsExactly<System.Text.Json.JsonException>();

    /// <summary>Verifies that an unknown enum name throws.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_ThrowForInvalidEnumValues() =>
        await Assert.That(
                () => SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
                    "\"notAValue\"",
                    SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()))
            .ThrowsExactly<System.Text.Json.JsonException>();

    /// <summary>Verifies that unexpected JSON tokens throw when parsing enums.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_ThrowForUnexpectedTokensWhenParsingEnums() =>
        await Assert.That(
                () => SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
                    "true",
                    SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()))
            .ThrowsExactly<System.Text.Json.JsonException>();

    /// <summary>Verifies that undefined enum values are serialized as numbers.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_SerializeUndefinedEnumValuesAsNumbers()
    {
        var json = SystemTextJsonSerializer.Serialize(
            (CamelCaseEnum)999,
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions());

        await Assert.That(json).IsEqualTo("999");
    }

    /// <summary>Verifies that undefined unsigned enum values are serialized as unsigned numbers.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_DefaultOptions_SerializeUndefinedUnsignedEnumValuesAsNumbers()
    {
        var json = SystemTextJsonSerializer.Serialize(
            (UnsignedCamelCaseEnum)9_223_372_036_854_775_808UL,
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
                [(CamelCaseEnum)999] = "unknown"
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
                [(UnsignedCamelCaseEnum)9_223_372_036_854_775_808UL] = "unknown"
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
            Name = "Road Runner",
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

    /// <summary>Verifies that RestService can use source-generated System.Text.Json metadata.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task RestService_CanUseSourceGeneratedSystemTextJsonMetadata()
    {
        var resolver = new TrackingTypeInfoResolver(SerializedContentJsonSerializerContext.Default);
        var settings = new RefitSettings(
            new SystemTextJsonContentSerializer(
                new(JsonSerializerDefaults.Web)
                {
                    TypeInfoResolver = resolver
                }))
        {
            HttpMessageHandlerFactory = () => new StubHttpMessageHandler(
                _ => Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\"name\":\"Road Runner\",\"company\":\"ACME\",\"createdAt\":\"1949-09-17\"}",
                            Encoding.UTF8,
                            "application/json")
                    }))
        };

        var api = RestService.For<IGitHubApi>(BaseAddress, settings);
        var user = await api.GetUser("roadrunner");

        await Assert.That(user).IsNotNull();
        await Assert.That(user.Name).IsEqualTo("Road Runner");
        await Assert.That(user.Company).IsEqualTo("ACME");
        await Assert.That(user.CreatedAt).IsEqualTo("1949-09-17");
        await Assert.That(resolver.RequestedTypes).Contains(typeof(User));
    }

    /// <summary>Verifies that a request body is serialized using the declared polymorphic base type.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task RestService_SerializesBodyUsingDeclaredPolymorphicBaseType()
    {
        string? serializedBody = null;
        var settings = new RefitSettings(
            new SystemTextJsonContentSerializer(
                new(JsonSerializerDefaults.Web)
                {
                    TypeInfoResolver = PolymorphicRequestJsonSerializerContext.Default
                }))
        {
            HttpMessageHandlerFactory = () => new StubHttpMessageHandler(async request =>
            {
                serializedBody = await request.Content!.ReadAsStringAsync();
                return new(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            })
        };

        var api = RestService.For<IPolymorphicRequestApi>(BaseAddress, settings);
        await api.CreateWeapon(new LaserWeaponRequest { Name = "Photon" });

        await Assert.That(serializedBody).IsNotNull();
        await Assert.That(serializedBody).Contains("\"$type\":\"laser\"");
        await Assert.That(serializedBody).Contains("\"name\":\"Photon\"");
    }

    /// <summary>Verifies resolver-provided polymorphism metadata is honored for declared abstract body types.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task RestService_SerializesBodyUsingResolverPolymorphismMetadata()
    {
        string? serializedBody = null;
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(
            typeInfo =>
            {
                if (typeInfo.Type != typeof(ResolverPolymorphicRequest))
                {
                    return;
                }

                typeInfo.PolymorphismOptions = new()
                {
                    TypeDiscriminatorPropertyName = "$type",
                    DerivedTypes =
                    {
                        new(typeof(ResolverLaserWeaponRequest), "laser")
                    }
                };
            });
        var settings = new RefitSettings(
            new SystemTextJsonContentSerializer(
                new(JsonSerializerDefaults.Web)
                {
                    TypeInfoResolver = resolver
                }))
        {
            HttpMessageHandlerFactory = () => new StubHttpMessageHandler(async request =>
            {
                serializedBody = await request.Content!.ReadAsStringAsync();
                return new(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            })
        };

        var api = RestService.For<IResolverPolymorphicRequestApi>(BaseAddress, settings);
        await api.CreateWeapon(new ResolverLaserWeaponRequest { Name = "Photon" });

        await Assert.That(serializedBody).IsNotNull();
        await Assert.That(serializedBody).Contains("\"$type\":\"laser\"");
        await Assert.That(serializedBody).Contains("\"name\":\"Photon\"");
    }

    /// <summary>Verifies that a request body uses the runtime type when the declared type is an interface.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task RestService_SerializesBodyUsingRuntimeTypeWhenDeclaredTypeIsInterface()
    {
        string? serializedBody = null;
        var settings = new RefitSettings(new SystemTextJsonContentSerializer())
        {
            HttpMessageHandlerFactory = () => new StubHttpMessageHandler(async request =>
            {
                serializedBody = await request.Content!.ReadAsStringAsync();
                return new(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            })
        };

        var api = RestService.For<IInterfaceRequestApi>(BaseAddress, settings);
        await api.CreateWeapon(new InterfaceLaserWeaponRequest { Name = "Photon" });

        await Assert.That(serializedBody).IsNotNull();
        await Assert.That(serializedBody).IsEqualTo("""{"name":"Photon"}""");
    }

    /// <summary>Verifies resolver-backed options use runtime metadata when an interface body has no polymorphism metadata.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task RestService_SerializesInterfaceBodyUsingRuntimeTypeWithResolver()
    {
        string? serializedBody = null;
        var settings = new RefitSettings(
            new SystemTextJsonContentSerializer(
                new(JsonSerializerDefaults.Web)
                {
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                }))
        {
            HttpMessageHandlerFactory = () => new StubHttpMessageHandler(async request =>
            {
                serializedBody = await request.Content!.ReadAsStringAsync();
                return new(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            })
        };

        var api = RestService.For<IInterfaceRequestApi>(BaseAddress, settings);
        await api.CreateWeapon(new InterfaceLaserWeaponRequest { Name = "Photon" });

        await Assert.That(serializedBody).IsNotNull();
        await Assert.That(serializedBody).IsEqualTo("""{"name":"Photon"}""");
    }

    /// <summary>Verifies that a request body uses the runtime type when the declared type is abstract.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task RestService_SerializesBodyUsingRuntimeTypeWhenDeclaredTypeIsAbstract()
    {
        string? serializedBody = null;
        var settings = new RefitSettings(new SystemTextJsonContentSerializer())
        {
            HttpMessageHandlerFactory = () => new StubHttpMessageHandler(async request =>
            {
                serializedBody = await request.Content!.ReadAsStringAsync();
                return new(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            })
        };

        var api = RestService.For<IAbstractRequestApi>(BaseAddress, settings);
        await api.CreateWeapon(new AbstractLaserWeaponRequest { Name = "Photon" });

        await Assert.That(serializedBody).IsNotNull();
        await Assert.That(serializedBody).IsEqualTo("""{"name":"Photon"}""");
    }

    /// <summary>Verifies that a bare object is serialized as empty JSON.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_SerializesBareObjectAsEmptyJson()
    {
        var serializer = new SystemTextJsonContentSerializer();

        var content = serializer.ToHttpContent(new object());
        var serialized = await content.ReadAsStringAsync();

        await Assert.That(serialized).IsEqualTo("{}");
    }

    /// <summary>Verifies that enum dictionary keys round-trip through the serializer.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTextJsonContentSerializer_RoundTripsEnumDictionaryKeys()
    {
        var serializer = new SystemTextJsonContentSerializer();

        var source = new Dictionary<CamelCaseEnum, string>
        {
            [CamelCaseEnum.ValueOne] = "first",
            [CamelCaseEnum.alreadyLowercase] = "second"
        };

        var content = serializer.ToHttpContent(source);
        var serialized = await content.ReadAsStringAsync();

        var roundTrip = await serializer.FromHttpContentAsync<Dictionary<CamelCaseEnum, string>>(
            new StringContent(serialized, Encoding.UTF8, "application/json"));

        await Assert.That(roundTrip).IsNotNull();
        await Assert.That(roundTrip![CamelCaseEnum.ValueOne]).IsEqualTo("first");
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
            new StringContent("{\"status\":\"totally-ready\"}", Encoding.UTF8, "application/json"));

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
            HttpMessageHandlerFactory = () => new StubHttpMessageHandler(_ =>
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\"status\":\"totally-ready\"}",
                            Encoding.UTF8,
                            "application/json")
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
                return new(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            })
        };

        var api = RestService.For<IIssue2083ColorApi>(BaseAddress, settings);
        await api.PostColorAsync(new() { Color = EnumMemberNameColor.Green });

        await Assert.That(serializedBody).IsEqualTo("""{"color":"GREEN"}""");
    }
#endif

    /// <summary>Runs the task to completion or until the timeout occurs.</summary>
    /// <param name="fixtureTask">The fixture task to run within the time limit.</param>
    /// <returns>The original fixture task once it completes or the timeout elapses.</returns>
    private static async Task<Task<User>> RunTaskWithATimeLimit(Task<User> fixtureTask)
    {
        var circuitBreakerTask = Task.Delay(TimeSpan.FromSeconds(30));
        await Task.WhenAny(fixtureTask, circuitBreakerTask);
        return fixtureTask;
    }

    /// <summary>Exercises the nullable enum converter's property-name read and write paths.</summary>
    /// <returns>The values read from property names and the JSON written through the converter.</returns>
    private static (CamelCaseEnum? EmptyNameValue, CamelCaseEnum? NamedValue, string Json) ReadAndWriteNullableEnumPropertyNames()
    {
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
        var converter = (System.Text.Json.Serialization.JsonConverter<CamelCaseEnum?>)options.GetConverter(
            typeof(CamelCaseEnum?));
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes("""{"":"empty","valueOne":"first"}"""));
        reader.Read();
        reader.Read();
        var emptyNameValue = converter.ReadAsPropertyName(ref reader, typeof(CamelCaseEnum?), options);
        reader.Read();
        reader.Read();
        var namedValue = converter.ReadAsPropertyName(ref reader, typeof(CamelCaseEnum?), options);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
#pragma warning disable CS8607 // The nullable converter intentionally supports null keys by writing an empty property name.
            converter.WriteAsPropertyName(writer, null, options);
#pragma warning restore CS8607
            writer.WriteStringValue("empty");
            converter.WriteAsPropertyName(writer, CamelCaseEnum.ValueOne, options);
            writer.WriteStringValue("first");
            writer.WriteEndObject();
        }

        return (emptyNameValue, namedValue, Encoding.UTF8.GetString(stream.ToArray()));
    }

    /// <summary>Writes nullable enum values through the converter directly.</summary>
    /// <returns>The JSON written by the converter.</returns>
    private static string WriteNullableEnumValues()
    {
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
        var converter = (System.Text.Json.Serialization.JsonConverter<CamelCaseEnum?>)options.GetConverter(
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

    /// <summary>Base request type used to verify polymorphic body serialization.</summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(LaserWeaponRequest), "laser")]
    public abstract class CreateWeaponRequest
    {
        /// <summary>Gets or sets the weapon name.</summary>
        public string? Name { get; set; }
    }

    /// <summary>Concrete laser weapon request derived from <see cref="CreateWeaponRequest"/>.</summary>
    public sealed class LaserWeaponRequest : CreateWeaponRequest;

    /// <summary>Concrete request implementing <see cref="InterfaceCreateWeaponRequest"/>.</summary>
    public sealed class InterfaceLaserWeaponRequest : InterfaceCreateWeaponRequest
    {
        /// <summary>Gets or sets the weapon name.</summary>
        public string? Name { get; set; }
    }

    /// <summary>Base request whose polymorphism metadata is supplied by a resolver in tests.</summary>
    public abstract class ResolverPolymorphicRequest
    {
        /// <summary>Gets or sets the weapon name.</summary>
        public string? Name { get; set; }
    }

    /// <summary>Concrete request used with resolver-provided polymorphism metadata.</summary>
    public sealed class ResolverLaserWeaponRequest : ResolverPolymorphicRequest;

    /// <summary>Marker abstract request used to verify serialization when the declared type is abstract.</summary>
    [SuppressMessage(
        "RoslynCommonAnalyzers",
        "SST1436:Add members to a type or remove it",
        Justification = "Intentional empty abstract fixture used to verify declared-type serialization behavior.")]
    public abstract class AbstractCreateWeaponRequest;

    /// <summary>Concrete request derived from <see cref="AbstractCreateWeaponRequest"/>.</summary>
    public sealed class AbstractLaserWeaponRequest : AbstractCreateWeaponRequest
    {
        /// <summary>Gets or sets the weapon name.</summary>
        public string? Name { get; set; }
    }

#if NET9_0_OR_GREATER
    /// <summary>Envelope carrying an <see cref="EnumMemberNameStatus"/> value.</summary>
    public sealed class EnumMemberNameEnvelope
    {
        /// <summary>Gets or sets the status value.</summary>
        public EnumMemberNameStatus Status { get; set; }
    }

    /// <summary>Envelope carrying an <see cref="EnumMemberNameColor"/> value.</summary>
    public sealed class EnumMemberNameColorEnvelope
    {
        /// <summary>Gets or sets the color value.</summary>
        public EnumMemberNameColor Color { get; set; }
    }
#endif

    /// <summary>Container with an <see cref="object"/> value used to verify object-value inference.</summary>
    public sealed class ObjectValueContainer
    {
        /// <summary>Gets or sets the boxed value.</summary>
        public object? Value { get; set; }
    }

    /// <summary>Source-generated serialization context for the <see cref="User"/> type.</summary>
    [JsonSerializable(typeof(User))]
    internal sealed partial class SerializedContentJsonSerializerContext : JsonSerializerContext;

    /// <summary>Source-generated serialization context for the polymorphic weapon request types.</summary>
    [JsonSerializable(typeof(CreateWeaponRequest))]
    [JsonSerializable(typeof(LaserWeaponRequest))]
    internal sealed partial class PolymorphicRequestJsonSerializerContext : JsonSerializerContext;

    /// <summary>Source-generated serialization context for the <see cref="ObjectValueContainer"/> type.</summary>
    [JsonSerializable(typeof(ObjectValueContainer))]
    [JsonSerializable(typeof(string))]
    internal sealed partial class ObjectValueContainerJsonSerializerContext : JsonSerializerContext;

    /// <summary>Mock handler that asserts on the streamed request content and returns a configurable response.</summary>
    private sealed class MockPushStreamContentHttpMessageHandler : HttpMessageHandler
    {
        /// <summary>Gets or sets the delegate that asserts on the request content and produces the response content.</summary>
        public required Func<PushStreamContent, Task<HttpContent>> Asserts { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var content = request.Content as PushStreamContent;
            await Assert.That(content).IsTypeOf<PushStreamContent>();
            await Assert.That(Asserts).IsNotNull();

            var responseContent = await Asserts(content!).ConfigureAwait(false);

            return new(HttpStatusCode.OK) { Content = responseContent };
        }
    }

    /// <summary>Type info resolver that records every type requested while delegating to an inner resolver.</summary>
    /// <param name="innerResolver">The resolver to delegate metadata lookups to.</param>
    private sealed class TrackingTypeInfoResolver(IJsonTypeInfoResolver innerResolver) : IJsonTypeInfoResolver
    {
        /// <summary>Gets the set of types requested from this resolver.</summary>
        public HashSet<Type> RequestedTypes { get; } = [];

        /// <summary>Gets the type info for the requested type and records the request.</summary>
        /// <param name="type">The type whose metadata is requested.</param>
        /// <param name="options">The serializer options in effect.</param>
        /// <returns>The type info, or null if the inner resolver has none.</returns>
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            RequestedTypes.Add(type);
            return innerResolver.GetTypeInfo(type, options);
        }
    }

    /// <summary>HTTP handler that delegates response production to a supplied responder delegate.</summary>
    /// <param name="responder">The delegate that produces a response for each request.</param>
    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => responder(request);
    }

    /// <summary>Model used to verify Newtonsoft field-name resolution.</summary>
    private sealed class NewtonsoftFieldNameModel
    {
        /// <summary>Gets or sets the aliased name property.</summary>
        [JsonProperty(PropertyName = "json_name")]
        public string? Name { get; set; }

        /// <summary>Gets or sets the unaliased property; present only so reflection can resolve a property without a JsonProperty attribute.</summary>
        public string? Unaliased { get; set; } = string.Empty;
    }

    /// <summary>Model used to verify System.Text.Json field-name resolution.</summary>
    private sealed class SystemTextFieldNameModel
    {
        /// <summary>Gets or sets the aliased name property.</summary>
        [JsonPropertyName("json_name")]
        public string? Name { get; set; }

        /// <summary>Gets or sets the unaliased property; present only so reflection can resolve a property without a JsonPropertyName attribute.</summary>
        public string? Unaliased { get; set; } = string.Empty;
    }

    /// <summary>HTTP content that only supports asynchronous reads, used to verify async-only deserialization.</summary>
    /// <param name="json">The JSON payload to serve as the content body.</param>
    private sealed class AsyncOnlyJsonContent(string json) : HttpContent
    {
        /// <summary>The UTF-8 encoded JSON payload.</summary>
        private readonly byte[] _bytes = Encoding.UTF8.GetBytes(json);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(_bytes, 0, _bytes.Length);

        protected override bool TryComputeLength(out long length)
        {
            length = _bytes.Length;
            return true;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromResult<Stream>(new AsyncOnlyReadStream(_bytes));
    }

    /// <summary>Read-only stream that throws on synchronous reads to verify async-only consumption.</summary>
    /// <param name="bytes">The bytes the stream exposes for reading.</param>
    private sealed class AsyncOnlyReadStream(byte[] bytes) : Stream
    {
        /// <summary>The backing memory stream that supplies the bytes.</summary>
        private readonly MemoryStream _inner = new(bytes, writable: false);

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => _inner.CanSeek;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => _inner.Length;

        /// <inheritdoc/>
        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        /// <inheritdoc/>
        public override void Flush() => _inner.Flush();

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException("Synchronous reads are intentionally not supported.");

        /// <inheritdoc/>
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) => _inner.ReadAsync(buffer, cancellationToken);

        /// <inheritdoc/>
        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) => _inner.ReadAsync(buffer, offset, count, cancellationToken);

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }
}
