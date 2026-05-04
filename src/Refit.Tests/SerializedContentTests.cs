using System;
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

public partial class SerializedContentTests
{
    const string BaseAddress = "https://api/";

    [Test]
    [Arguments(typeof(NewtonsoftJsonContentSerializer))]
    [Arguments(typeof(SystemTextJsonContentSerializer))]
    [Arguments(typeof(XmlContentSerializer))]
    public async Task WhenARequestRequiresABodyThenItDoesNotDeadlock(Type contentSerializerType)
    {
        if (
            Activator.CreateInstance(contentSerializerType) is not IHttpContentSerializer serializer
        )
        {
            throw new ArgumentException(
                $"{contentSerializerType.FullName} does not implement {nameof(IHttpContentSerializer)}"
            );
        }

        var handler = new MockPushStreamContentHttpMessageHandler
        {
            Asserts = async content =>
                new StringContent(await content.ReadAsStringAsync().ConfigureAwait(false))
        };

        var settings = new RefitSettings(serializer) { HttpMessageHandlerFactory = () => handler };

        var fixture = RestService.For<IGitHubApi>(BaseAddress, settings);

        var fixtureTask = await RunTaskWithATimeLimit(fixture.CreateUser(new User()))
            .ConfigureAwait(false);
        Assert.True(fixtureTask.IsCompleted);
        Assert.Equal(TaskStatus.RanToCompletion, fixtureTask.Status);
    }

    [Test]
    [Arguments(typeof(NewtonsoftJsonContentSerializer))]
    [Arguments(typeof(SystemTextJsonContentSerializer))]
    [Arguments(typeof(XmlContentSerializer))]
    public async Task WhenARequestRequiresABodyThenItIsSerialized(Type contentSerializerType)
    {
        if (
            Activator.CreateInstance(contentSerializerType) is not IHttpContentSerializer serializer
        )
        {
            throw new ArgumentException(
                $"{contentSerializerType.FullName} does not implement {nameof(IHttpContentSerializer)}"
            );
        }

        var model = new User
        {
            Name = "Wile E. Coyote",
            CreatedAt = new DateTime(1949, 9, 16).ToString(),
            Company = "ACME",
        };

        var handler = new MockPushStreamContentHttpMessageHandler
        {
            Asserts = async content =>
            {
                var stringContent = new StringContent(
                    await content.ReadAsStringAsync().ConfigureAwait(false)
                );
                var user = await serializer
                    .FromHttpContentAsync<User>(content)
                    .ConfigureAwait(false);
                Assert.NotSame(model, user);
                Assert.Equal(model.Name, user.Name);
                Assert.Equal(model.CreatedAt, user.CreatedAt);
                Assert.Equal(model.Company, user.Company);

                //  return some content so that the serializer doesn't complain
                return stringContent;
            }
        };

        var settings = new RefitSettings(serializer) { HttpMessageHandlerFactory = () => handler };

        var fixture = RestService.For<IGitHubApi>(BaseAddress, settings);

        var fixtureTask = await RunTaskWithATimeLimit(fixture.CreateUser(model))
            .ConfigureAwait(false);

        Assert.True(fixtureTask.IsCompleted);
    }

    [Test]
    public void VerityDefaultSerializer()
    {
        var settings = new RefitSettings();

        Assert.NotNull(settings.ContentSerializer);
        Assert.IsType<SystemTextJsonContentSerializer>(settings.ContentSerializer);

        settings = new RefitSettings(new NewtonsoftJsonContentSerializer());

        Assert.NotNull(settings.ContentSerializer);
        Assert.IsType<NewtonsoftJsonContentSerializer>(settings.ContentSerializer);
    }

    /// <summary>
    /// Runs the task to completion or until the timeout occurs
    /// </summary>
    static async Task<Task<User>> RunTaskWithATimeLimit(Task<User> fixtureTask)
    {
        var circuitBreakerTask = Task.Delay(TimeSpan.FromSeconds(30));
        await Task.WhenAny(fixtureTask, circuitBreakerTask);
        return fixtureTask;
    }

    class MockPushStreamContentHttpMessageHandler : HttpMessageHandler
    {
        public Func<PushStreamContent, Task<HttpContent>> Asserts { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var content = request.Content as PushStreamContent;
            Assert.IsType<PushStreamContent>(content);
            Assert.NotNull(Asserts);

            var responseContent = await Asserts(content).ConfigureAwait(false);

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent };
        }
    }

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

        Assert.NotNull(result);
        Assert.Equal(model.ShortNameForAlias, result.ShortNameForAlias);
        Assert.Equal(model.ShortNameForJsonProperty, result.ShortNameForJsonProperty);
    }

    [Test]
    public void StreamDeserialization_UsingSystemTextJsonContentSerializer_SetsCorrectHeaders()
    {
        var model = new TestAliasObject
        {
            ShortNameForAlias = nameof(StreamDeserialization_UsingSystemTextJsonContentSerializer),
            ShortNameForJsonProperty = nameof(TestAliasObject)
        };

        var serializer = new SystemTextJsonContentSerializer();

        var json = serializer.ToHttpContent(model);

        Assert.NotNull(json.Headers.ContentType);
        Assert.Equal("utf-8", json.Headers.ContentType.CharSet);
        Assert.Equal("application/json", json.Headers.ContentType.MediaType);
    }

    [Test]
    public async Task StreamDeserialization_UsingNewtonsoftJsonContentSerializer_DoesNotUseSynchronousReads()
    {
        var serializer = new NewtonsoftJsonContentSerializer();
        var content = new AsyncOnlyJsonContent("{\"name\":\"Road Runner\"}");

        var result = await serializer.FromHttpContentAsync<User>(content);

        Assert.NotNull(result);
        Assert.Equal("Road Runner", result.Name);
    }

    [Test]
    public async Task StreamDeserialization_UsingNewtonsoftJsonContentSerializer_ReturnsDefaultForNullContent()
    {
        var serializer = new NewtonsoftJsonContentSerializer();

        var result = await serializer.FromHttpContentAsync<User>(null!);

        Assert.Null(result);
    }

    [Test]
    public void NewtonsoftJsonContentSerializer_GetFieldNameForProperty_ReturnsJsonPropertyName()
    {
        var serializer = new NewtonsoftJsonContentSerializer();
        var property = typeof(NewtonsoftFieldNameModel).GetProperty(
            nameof(NewtonsoftFieldNameModel.Name)
        );

        var fieldName = serializer.GetFieldNameForProperty(property!);

        Assert.Equal("json_name", fieldName);
    }

    [Test]
    public void NewtonsoftJsonContentSerializer_GetFieldNameForProperty_ReturnsNullWithoutJsonPropertyAttribute()
    {
        var serializer = new NewtonsoftJsonContentSerializer();
        var property = typeof(NewtonsoftFieldNameModel).GetProperty(
            nameof(NewtonsoftFieldNameModel.Unaliased)
        );

        var fieldName = serializer.GetFieldNameForProperty(property!);

        Assert.Null(fieldName);
    }

    [Test]
    public void NewtonsoftJsonContentSerializer_GetFieldNameForProperty_ThrowsForNullProperty()
    {
        var serializer = new NewtonsoftJsonContentSerializer();

        var exception = Assert.Throws<ArgumentNullException>(() => serializer.GetFieldNameForProperty(null!));

        Assert.Equal("propertyInfo", exception.ParamName);
    }

    [Test]
    public void SystemTextJsonContentSerializer_GetFieldNameForProperty_ReturnsJsonPropertyName()
    {
        var serializer = new SystemTextJsonContentSerializer();
        var property = typeof(SystemTextFieldNameModel).GetProperty(
            nameof(SystemTextFieldNameModel.Name)
        );

        var fieldName = serializer.GetFieldNameForProperty(property!);

        Assert.Equal("json_name", fieldName);
    }

    [Test]
    public void SystemTextJsonContentSerializer_GetFieldNameForProperty_ReturnsNullWithoutJsonPropertyNameAttribute()
    {
        var serializer = new SystemTextJsonContentSerializer();
        var property = typeof(SystemTextFieldNameModel).GetProperty(
            nameof(SystemTextFieldNameModel.Unaliased)
        );

        var fieldName = serializer.GetFieldNameForProperty(property!);

        Assert.Null(fieldName);
    }

    [Test]
    public void SystemTextJsonContentSerializer_GetFieldNameForProperty_ThrowsForNullProperty()
    {
        var serializer = new SystemTextJsonContentSerializer();

        var exception = Assert.Throws<ArgumentNullException>(() => serializer.GetFieldNameForProperty(null!));

        Assert.Equal("propertyInfo", exception.ParamName);
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_InferBooleanObjectValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>(
            """{"value":true}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.True(Assert.IsType<bool>(result!.Value));
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_InferIntegralObjectValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>(
            """{"value":42}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Equal(42L, Assert.IsType<long>(result!.Value));
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_InferFloatingPointObjectValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>(
            """{"value":42.5}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Equal(42.5, Assert.IsType<double>(result!.Value));
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_InferDateObjectValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>(
            """{"value":"2024-01-02T03:04:05Z"}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Equal(
            new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            Assert.IsType<DateTime>(result!.Value)
        );
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_InferStringObjectValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>(
            """{"value":"Road Runner"}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Equal("Road Runner", Assert.IsType<string>(result!.Value));
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_DeserializeObjectValuesAsJsonElements()
    {
        var result = SystemTextJsonSerializer.Deserialize<ObjectValueContainer>(
            """{"value":{"company":"ACME"}}""",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Equal(JsonValueKind.Object, Assert.IsType<JsonElement>(result!.Value).ValueKind);
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_SerializeObjectEnumValuesAsCamelCase()
    {
        var json = SystemTextJsonSerializer.Serialize(
            new ObjectValueContainer { Value = CamelCaseEnum.ValueOne },
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Equal("""{"value":"valueOne"}""", json);
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_SerializeNullObjectValuesAsJsonNull()
    {
        var json = SystemTextJsonSerializer.Serialize(
            new ObjectValueContainer { Value = null },
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Equal("""{"value":null}""", json);
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_UseResolverWhenSerializingObjectValues()
    {
        var resolver = new TrackingTypeInfoResolver(ObjectValueContainerJsonSerializerContext.Default);
        var options = new JsonSerializerOptions(
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        )
        {
            TypeInfoResolver = resolver
        };

        var json = SystemTextJsonSerializer.Serialize(
            new ObjectValueContainer { Value = "Road Runner" },
            options
        );

        Assert.Equal("""{"value":"Road Runner"}""", json);
        Assert.Contains(typeof(string), resolver.RequestedTypes);
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_DeserializeCamelCaseEnumValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
            "\"valueOne\"",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Equal(CamelCaseEnum.ValueOne, result);
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_DeserializeCaseInsensitiveEnumValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
            "\"VALUEONE\"",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Equal(CamelCaseEnum.ValueOne, result);
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_DeserializeLowercaseEnumValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
            "\"alreadyLowercase\"",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Equal(CamelCaseEnum.alreadyLowercase, result);
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_DeserializeNumericEnumValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
            "2",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Equal(CamelCaseEnum.alreadyLowercase, result);
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_DeserializeNullNullableEnumValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum?>(
            "null",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Null(result);
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_DeserializeEmptyNullableEnumValues()
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum?>(
            "\"\"",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Null(result);
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_ThrowForNullNonNullableEnumValues()
    {
        Assert.Throws<System.Text.Json.JsonException>(
            () => SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
                "null",
                SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
            )
        );
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_ThrowForEmptyNonNullableEnumValues()
    {
        Assert.Throws<System.Text.Json.JsonException>(
            () => SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
                "\"\"",
                SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
            )
        );
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_ThrowForInvalidEnumValues()
    {
        Assert.Throws<System.Text.Json.JsonException>(
            () => SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
                "\"notAValue\"",
                SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
            )
        );
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_ThrowForUnexpectedTokensWhenParsingEnums()
    {
        Assert.Throws<System.Text.Json.JsonException>(
            () => SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
                "true",
                SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
            )
        );
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_SerializeUndefinedEnumValuesAsNumbers()
    {
        var json = SystemTextJsonSerializer.Serialize(
            (CamelCaseEnum)999,
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Equal("999", json);
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_SerializeLowercaseEnumNamesUnchanged()
    {
        var json = SystemTextJsonSerializer.Serialize(
            CamelCaseEnum.alreadyLowercase,
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Equal("\"alreadyLowercase\"", json);
    }

    [Test]
    [Arguments("vAlUeOnE")]
    [Arguments("ValueOne")]
    [Arguments("VALUEONE")]
    [Arguments("valueone")]
    public void SystemTextJsonContentSerializer_DefaultOptions_DeserializesEnumValuesWithVariousCasings(
        string jsonValue
    )
    {
        var result = SystemTextJsonSerializer.Deserialize<CamelCaseEnum>(
            $"\"{jsonValue}\"",
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        Assert.Equal(CamelCaseEnum.ValueOne, result);
    }

    [Test]
    public void SystemTextJsonContentSerializer_DefaultOptions_ExactCaseMatchTakesPriorityOverCaseInsensitiveWhenMembersDifferByCase()
    {
        // When enum has members whose names differ only by case, the exact serialized form
        // (camelCase) should be used first (case-sensitive), falling back to case-insensitive only
        // for inputs that do not exactly match any known serialized form.

        // CaseDifferentMembers.Alpha serializes to "alpha" (camelCase),
        // CaseDifferentMembers.ALPHA serializes to "aLPHA" (camelCase).
        // Exact-match lookups must correctly disambiguate these.
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();

        Assert.Equal(
            CaseDifferentMembers.Alpha,
            SystemTextJsonSerializer.Deserialize<CaseDifferentMembers>("\"alpha\"", options)
        );
        Assert.Equal(
            CaseDifferentMembers.ALPHA,
            SystemTextJsonSerializer.Deserialize<CaseDifferentMembers>("\"aLPHA\"", options)
        );
        // Field names are also accepted via exact match
        Assert.Equal(
            CaseDifferentMembers.Alpha,
            SystemTextJsonSerializer.Deserialize<CaseDifferentMembers>("\"Alpha\"", options)
        );
        Assert.Equal(
            CaseDifferentMembers.ALPHA,
            SystemTextJsonSerializer.Deserialize<CaseDifferentMembers>("\"ALPHA\"", options)
        );
    }

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

        Assert.NotNull(roundTrip);
        Assert.Equal(model.Name, roundTrip.Name);
        Assert.Equal(model.Company, roundTrip.Company);
        Assert.Equal(model.CreatedAt, roundTrip.CreatedAt);
        Assert.Contains(typeof(User), resolver.RequestedTypes);
    }

    [Test]
    public async Task RestService_CanUseSourceGeneratedSystemTextJsonMetadata()
    {
        var resolver = new TrackingTypeInfoResolver(SerializedContentJsonSerializerContext.Default);
        var settings = new RefitSettings(
            new SystemTextJsonContentSerializer(
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    TypeInfoResolver = resolver
                }
            )
        )
        {
            HttpMessageHandlerFactory = () => new StubHttpMessageHandler(
                _ => Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\"name\":\"Road Runner\",\"company\":\"ACME\",\"createdAt\":\"1949-09-17\"}",
                            Encoding.UTF8,
                            "application/json"
                        )
                    }
                )
            )
        };

        var api = RestService.For<IGitHubApi>(BaseAddress, settings);
        var user = await api.GetUser("roadrunner");

        Assert.NotNull(user);
        Assert.Equal("Road Runner", user.Name);
        Assert.Equal("ACME", user.Company);
        Assert.Equal("1949-09-17", user.CreatedAt);
        Assert.Contains(typeof(User), resolver.RequestedTypes);
    }

    [Test]
    public async Task RestService_SerializesBodyUsingDeclaredPolymorphicBaseType()
    {
        string? serializedBody = null;
        var settings = new RefitSettings(
            new SystemTextJsonContentSerializer(
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    TypeInfoResolver = PolymorphicRequestJsonSerializerContext.Default
                }
            )
        )
        {
            HttpMessageHandlerFactory = () => new StubHttpMessageHandler(async request =>
            {
                serializedBody = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            })
        };

        var api = RestService.For<IPolymorphicRequestApi>(BaseAddress, settings);
        await api.CreateWeapon(new LaserWeaponRequest { Name = "Photon" });

        Assert.NotNull(serializedBody);
        Assert.Contains("\"$type\":\"laser\"", serializedBody, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"Photon\"", serializedBody, StringComparison.Ordinal);
    }

#if NET9_0_OR_GREATER
    [Test]
    public async Task SystemTextJsonContentSerializer_SupportsJsonStringEnumMemberName()
    {
        var serializer = new SystemTextJsonContentSerializer(
            SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
        );

        var content = serializer.ToHttpContent(
            new EnumMemberNameEnvelope { Status = EnumMemberNameStatus.TotallyReady }
        );
        var serialized = await content.ReadAsStringAsync();
        var roundTrip = await serializer.FromHttpContentAsync<EnumMemberNameEnvelope>(
            new StringContent("{\"status\":\"totally-ready\"}", Encoding.UTF8, "application/json")
        );

        Assert.Contains("totally-ready", serialized, StringComparison.Ordinal);
        Assert.NotNull(roundTrip);
        Assert.Equal(EnumMemberNameStatus.TotallyReady, roundTrip.Status);
    }

    [Test]
    public async Task RestService_UsesDefaultEnumConverterWithJsonStringEnumMemberName()
    {
        var settings = new RefitSettings(
            new SystemTextJsonContentSerializer(
                SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions()
            )
        )
        {
            HttpMessageHandlerFactory = () => new StubHttpMessageHandler(_ =>
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\"status\":\"totally-ready\"}",
                            Encoding.UTF8,
                            "application/json"
                        )
                    }
                )
            )
        };

        var api = RestService.For<IIssue2067StatusApi>(BaseAddress, settings);
        var result = await api.GetStatusAsync();

        Assert.Equal(EnumMemberNameStatus.TotallyReady, result.Status);
    }

    [Test]
    public async Task RestService_DefaultSystemTextJsonSerializerHonorsJsonStringEnumMemberNameWithAttributedConverter()
    {
        string serializedBody = string.Empty;
        var settings = new RefitSettings(new SystemTextJsonContentSerializer())
        {
            HttpMessageHandlerFactory = () => new StubHttpMessageHandler(async request =>
            {
                serializedBody = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            })
        };

        var api = RestService.For<IIssue2083ColorApi>(BaseAddress, settings);
        await api.PostColorAsync(new EnumMemberNameColorEnvelope { Color = EnumMemberNameColor.Green });

        Assert.Equal("""{"color":"GREEN"}""", serializedBody);
    }
#endif

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(LaserWeaponRequest), "laser")]
    public abstract class CreateWeaponRequest
    {
        public string? Name { get; set; }
    }

    public sealed class LaserWeaponRequest : CreateWeaponRequest { }

    public interface IPolymorphicRequestApi
    {
        [Post("/weapons")]
        Task CreateWeapon(CreateWeaponRequest request);
    }

#if NET9_0_OR_GREATER
    public enum EnumMemberNameStatus
    {
        [JsonStringEnumMemberName("totally-ready")]
        TotallyReady,

        NeedsReview
    }

    public sealed class EnumMemberNameEnvelope
    {
        public EnumMemberNameStatus Status { get; set; }
    }

    public interface IIssue2067StatusApi
    {
        [Get("/status")]
        Task<EnumMemberNameEnvelope> GetStatusAsync();
    }

    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter<EnumMemberNameColor>))]
    public enum EnumMemberNameColor
    {
        [JsonStringEnumMemberName("GREEN")]
        Green,

        [JsonStringEnumMemberName("RED")]
        Red
    }

    public sealed class EnumMemberNameColorEnvelope
    {
        public EnumMemberNameColor Color { get; set; }
    }

    public interface IIssue2083ColorApi
    {
        [Post("/color")]
        Task PostColorAsync([Body] EnumMemberNameColorEnvelope body);
    }
#endif

    [JsonSerializable(typeof(User))]
    internal sealed partial class SerializedContentJsonSerializerContext : JsonSerializerContext { }

    [JsonSerializable(typeof(CreateWeaponRequest))]
    [JsonSerializable(typeof(LaserWeaponRequest))]
    internal sealed partial class PolymorphicRequestJsonSerializerContext : JsonSerializerContext { }

    [JsonSerializable(typeof(ObjectValueContainer))]
    [JsonSerializable(typeof(string))]
    internal sealed partial class ObjectValueContainerJsonSerializerContext : JsonSerializerContext { }

    sealed class TrackingTypeInfoResolver(IJsonTypeInfoResolver innerResolver) : IJsonTypeInfoResolver
    {
        public HashSet<Type> RequestedTypes { get; } = [];

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            RequestedTypes.Add(type);
            return innerResolver.GetTypeInfo(type, options);
        }
    }

    sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => responder(request);
    }

    sealed class NewtonsoftFieldNameModel
    {
        [JsonProperty(PropertyName = "json_name")]
        public string Name { get; set; }

        public string Unaliased { get; set; }
    }

    sealed class SystemTextFieldNameModel
    {
        [JsonPropertyName("json_name")]
        public string Name { get; set; }

        public string Unaliased { get; set; }
    }

    public sealed class ObjectValueContainer
    {
        public object Value { get; set; }
    }

    enum CamelCaseEnum
    {
        ValueOne = 1,
        alreadyLowercase = 2
    }

    // Members Alpha and ALPHA differ only by case; this enum is used to verify that
    // the case-sensitive lookup takes priority and the correct member is chosen.
    enum CaseDifferentMembers
    {
        Alpha = 1,
        ALPHA = 2,
    }

    sealed class AsyncOnlyJsonContent(string json) : HttpContent
    {
        readonly byte[] _bytes = Encoding.UTF8.GetBytes(json);

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

    sealed class AsyncOnlyReadStream(byte[] bytes) : Stream
    {
        readonly MemoryStream _inner = new(bytes, writable: false);

        public override bool CanRead => true;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException("Synchronous reads are intentionally not supported.");

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default
        ) => _inner.ReadAsync(buffer, cancellationToken);

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        ) => _inner.ReadAsync(buffer, offset, count, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }
}
