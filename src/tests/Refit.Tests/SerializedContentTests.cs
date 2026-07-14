// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
namespace Refit.Tests;

/// <summary>Tests that verify how Refit serializes request bodies and deserializes responses across content serializers.</summary>
public partial class SerializedContentTests
{
    /// <summary>The base address used when creating Refit clients for these tests.</summary>
    private const string BaseAddress = "https://api/";

    /// <summary>The JSON media type used for serialized request and response content.</summary>
    private const string JsonMediaType = "application/json";

    /// <summary>A sample name reused across serialization round-trip assertions.</summary>
    private const string RoadRunnerName = "Road Runner";

    /// <summary>A sample weapon name reused across polymorphic body serialization assertions.</summary>
    private const string PhotonName = "Photon";

    /// <summary>The serialized JSON body asserted across the polymorphic serialization tests.</summary>
    private const string PhotonJson = """{"name":"Photon"}""";

    /// <summary>The first enum dictionary value reused across the camel-case serialization tests.</summary>
    private const string FirstEnumValue = "first";

    /// <summary>The expected inferred integral value for object-value inference tests.</summary>
    private const long ExpectedIntegralValue = 42L;

    /// <summary>The expected inferred floating-point value for object-value inference tests.</summary>
    private const double ExpectedFloatingPointValue = 42.5;

    /// <summary>The expected identifier parsed from a numeric JSON string.</summary>
    private const int ExpectedId = 123;

    /// <summary>The expected amount parsed from a numeric JSON string.</summary>
    private const decimal ExpectedAmount = 9.99M;

    /// <summary>The expected year component of the inferred date value.</summary>
    private const int ExpectedYear = 2024;

    /// <summary>The expected day component of the inferred date value.</summary>
    private const int ExpectedDay = 2;

    /// <summary>The expected hour component of the inferred date value.</summary>
    private const int ExpectedHour = 3;

    /// <summary>The expected minute component of the inferred date value.</summary>
    private const int ExpectedMinute = 4;

    /// <summary>The expected second component of the inferred date value.</summary>
    private const int ExpectedSecond = 5;

    /// <summary>The year component of the sample created-at date used in body-serialization round-trips.</summary>
    private const int SampleCreatedYear = 1949;

    /// <summary>The month component of the sample created-at date used in body-serialization round-trips.</summary>
    private const int SampleCreatedMonth = 9;

    /// <summary>The day component of the sample created-at date used in body-serialization round-trips.</summary>
    private const int SampleCreatedDay = 16;

    /// <summary>An undefined signed enum value used to verify serialization of unknown enum members.</summary>
    private const int UndefinedEnumValue = 999;

    /// <summary>An undefined unsigned enum value (2^63) used to verify serialization of unknown unsigned enum members.</summary>
    private const ulong UndefinedUnsignedEnumValue = 9_223_372_036_854_775_808UL;

    /// <summary>The circuit-breaker timeout, in seconds, that bounds a body-serialization task run.</summary>
    private const int CircuitBreakerTimeoutSeconds = 30;

    /// <summary>Serializer options carrying the source-generated polymorphic context, shared by the declared-base body test.</summary>
    private static readonly JsonSerializerOptions PolymorphicBaseSerializerOptions =
        new(JsonSerializerDefaults.Web) { TypeInfoResolver = PolymorphicRequestJsonSerializerContext.Default };

    /// <summary>Reflection-backed serializer options shared by the interface-body runtime-type test.</summary>
    private static readonly JsonSerializerOptions ReflectionSerializerOptions =
        new(JsonSerializerDefaults.Web) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

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
            Asserts = static async content =>
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
            CreatedAt = new DateOnly(SampleCreatedYear, SampleCreatedMonth, SampleCreatedDay).ToString(),
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
        await Assert.That(json.Headers.ContentType.MediaType).IsEqualTo(JsonMediaType);
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
            HttpMessageHandlerFactory = static () => new StubHttpMessageHandler(
                static _ => Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            "{\"name\":\"Road Runner\",\"company\":\"ACME\",\"createdAt\":\"1949-09-17\"}",
                            Encoding.UTF8,
                            JsonMediaType)
                    }))
        };

        var api = RestService.For<IGitHubApi>(BaseAddress, settings);
        var user = await api.GetUser("roadrunner");

        await Assert.That(user).IsNotNull();
        await Assert.That(user.Name).IsEqualTo(RoadRunnerName);
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
            new SystemTextJsonContentSerializer(PolymorphicBaseSerializerOptions))
        {
            HttpMessageHandlerFactory = () => new StubHttpMessageHandler(async request =>
            {
                serializedBody = await request.Content!.ReadAsStringAsync();
                return new(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, JsonMediaType)
                };
            })
        };

        var api = RestService.For<IPolymorphicRequestApi>(BaseAddress, settings);
        await api.CreateWeapon(new LaserWeaponRequest { Name = PhotonName });

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
            static typeInfo =>
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
                    Content = new StringContent("{}", Encoding.UTF8, JsonMediaType)
                };
            })
        };

        var api = RestService.For<IResolverPolymorphicRequestApi>(BaseAddress, settings);
        await api.CreateWeapon(new ResolverLaserWeaponRequest { Name = PhotonName });

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
                    Content = new StringContent("{}", Encoding.UTF8, JsonMediaType)
                };
            })
        };

        var api = RestService.For<IInterfaceRequestApi>(BaseAddress, settings);
        await api.CreateWeapon(new InterfaceLaserWeaponRequest { Name = PhotonName });

        await Assert.That(serializedBody).IsNotNull();
        await Assert.That(serializedBody).IsEqualTo(PhotonJson);
    }

    /// <summary>Verifies resolver-backed options use runtime metadata when an interface body has no polymorphism metadata.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task RestService_SerializesInterfaceBodyUsingRuntimeTypeWithResolver()
    {
        string? serializedBody = null;
        var settings = new RefitSettings(
            new SystemTextJsonContentSerializer(ReflectionSerializerOptions))
        {
            HttpMessageHandlerFactory = () => new StubHttpMessageHandler(async request =>
            {
                serializedBody = await request.Content!.ReadAsStringAsync();
                return new(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, JsonMediaType)
                };
            })
        };

        var api = RestService.For<IInterfaceRequestApi>(BaseAddress, settings);
        await api.CreateWeapon(new InterfaceLaserWeaponRequest { Name = PhotonName });

        await Assert.That(serializedBody).IsNotNull();
        await Assert.That(serializedBody).IsEqualTo(PhotonJson);
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
                    Content = new StringContent("{}", Encoding.UTF8, JsonMediaType)
                };
            })
        };

        var api = RestService.For<IAbstractRequestApi>(BaseAddress, settings);
        await api.CreateWeapon(new AbstractLaserWeaponRequest { Name = PhotonName });

        await Assert.That(serializedBody).IsNotNull();
        await Assert.That(serializedBody).IsEqualTo(PhotonJson);
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

    /// <summary>Runs the task to completion or until the timeout occurs.</summary>
    /// <param name="fixtureTask">The fixture task to run within the time limit.</param>
    /// <returns>The original fixture task once it completes or the timeout elapses.</returns>
    private static async Task<Task<User>> RunTaskWithATimeLimit(Task<User> fixtureTask)
    {
        var circuitBreakerTask = Task.Delay(TimeSpan.FromSeconds(CircuitBreakerTimeoutSeconds));
        await Task.WhenAny(fixtureTask, circuitBreakerTask);
        return fixtureTask;
    }

    /// <summary>Base request type used to verify polymorphic body serialization.</summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(LaserWeaponRequest), "laser")]
    public class CreateWeaponRequest
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
    public class ResolverPolymorphicRequest
    {
        /// <summary>Gets or sets the weapon name.</summary>
        public string? Name { get; set; }
    }

    /// <summary>Concrete request used with resolver-provided polymorphism metadata.</summary>
    public sealed class ResolverLaserWeaponRequest : ResolverPolymorphicRequest;

    /// <summary>Marker abstract request used to verify serialization when the declared type is abstract.</summary>
    public abstract class AbstractCreateWeaponRequest
    {
        /// <summary>Names the concrete weapon kind; a contract member the serializer never emits because it ignores methods.</summary>
        /// <returns>The concrete weapon discriminator.</returns>
        public abstract string DescribeWeapon();

        /// <summary>Returns the request's type name; a concrete member so this stays an abstract base class rather than an interface.</summary>
        /// <returns>The type name of the request.</returns>
        public override string ToString() => nameof(AbstractCreateWeaponRequest);
    }

    /// <summary>Concrete request derived from <see cref="AbstractCreateWeaponRequest"/>.</summary>
    public sealed class AbstractLaserWeaponRequest : AbstractCreateWeaponRequest
    {
        /// <summary>Gets or sets the weapon name.</summary>
        public string? Name { get; set; }

        /// <inheritdoc/>
        public override string DescribeWeapon() => "laser";
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

    /// <summary>Container with numeric values used to verify <see cref="JsonNumberHandling.AllowReadingFromString"/> behavior.</summary>
    public sealed class NumberContainer
    {
        /// <summary>Gets or sets an integral value.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets a decimal value.</summary>
        public decimal Amount { get; set; }
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
            _ = RequestedTypes.Add(type);
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

    /// <summary>Model used to verify System.Text.Json field-name resolution.</summary>
    private sealed class SystemTextFieldNameModel
    {
        /// <summary>Gets or sets the aliased name property.</summary>
        [JsonPropertyName("json_name")]
        public string? Name { get; set; }

        /// <summary>Gets or sets the unaliased property; present only so reflection can resolve a property without a JsonPropertyName attribute.</summary>
        public string? Unaliased { get; set; } = string.Empty;
    }
}
