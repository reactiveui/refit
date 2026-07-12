// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Refit.Buffers;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Tests covering response deserialization, error handling, and exception hydration behavior.</summary>
public sealed class ResponseTests
{
    /// <summary>Base address shared by the response-handling fixtures.</summary>
    private const string BaseAddress = "http://api";

    /// <summary>Endpoint URL for the alias test operation.</summary>
    private const string AliasTestUrl = BaseAddress + "/aliasTest";

    /// <summary>Endpoint URL for the API response test operation.</summary>
    private const string GetApiResponseTestObjectUrl = BaseAddress + "/GetApiResponseTestObject";

    /// <summary>Media type used for RFC 7807 problem-details responses.</summary>
    private const string ProblemJsonMediaType = "application/problem+json";

    /// <summary>The JSON media type reused across the content-type tests.</summary>
    private const string JsonMediaType = "application/json";

    /// <summary>The invalid JSON response body used to trigger deserialization failures.</summary>
    private const string InvalidJsonContent = "Invalid JSON";

    /// <summary>Expected problem-details detail value.</summary>
    private const string DetailValue = "detail";

    /// <summary>Expected problem-details instance value.</summary>
    private const string InstanceValue = "instance";

    /// <summary>Expected problem-details title value.</summary>
    private const string TitleValue = "title";

    /// <summary>First validation error field key.</summary>
    private const string FieldOneKey = "Field1";

    /// <summary>Second validation error field key.</summary>
    private const string FieldTwoKey = "Field2";

    /// <summary>Plain-text body used by the empty-content fixtures.</summary>
    private const string HelloWorldContent = "Hello world";

    /// <summary>Expected number of hydrated problem-details extensions.</summary>
    private const int ExpectedExtensionsCount = 2;

    /// <summary>Problem detail entries for the first validation problem fixture.</summary>
    private static readonly string[] _field1Problems = ["Problem1"];

    /// <summary>Problem detail entries for the second validation problem fixture.</summary>
    private static readonly string[] _field2Problems = ["Problem2"];

    /// <summary>Refit service used to exercise alias and response handling.</summary>
    public interface IMyAliasService
    {
        /// <summary>Gets the aliased test object from the test endpoint.</summary>
        /// <returns>The deserialized <see cref="TestAliasObject"/>.</returns>
        [Get("/aliasTest")]
        Task<TestAliasObject?> GetTestObject();

        /// <summary>Gets the test object wrapped in an <see cref="ApiResponse{T}"/>.</summary>
        /// <returns>The wrapped response.</returns>
        [Get("/GetApiResponseTestObject")]
        Task<ApiResponse<TestAliasObject>?> GetApiResponseTestObject();

        /// <summary>Gets a non-generic <see cref="IApiResponse"/> from the test endpoint.</summary>
        /// <returns>The response.</returns>
        [Get("/GetIApiResponse")]
        Task<IApiResponse> GetIApiResponse();

        /// <summary>Gets a non-generic <see cref="IApiResponse"/> returned as a <see cref="ValueTask{T}"/>.</summary>
        /// <returns>The response.</returns>
        [Get("/GetValueTaskIApiResponse")]
        ValueTask<IApiResponse> GetValueTaskIApiResponse();
    }

    /// <summary>Verifies that JsonProperty can be used to alias field names in responses.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task JsonPropertyCanBeUsedToAliasFieldNamesInResponses()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(AliasTestUrl),
                Reply.Json("{\"FIELD_WE_SHOULD_SHORTEN_WITH_ALIAS_AS\": \"Hello\", \"FIELD_WE_SHOULD_SHORTEN_WITH_JSON_PROPERTY\": \"World\"}")
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        var result = await fixture.GetTestObject();

        await Assert.That(result!.ShortNameForJsonProperty).IsEqualTo("World");
    }

    /// <summary>
    /// Even though it may seem like AliasAs and JsonProperty are used interchangeably in some places,
    /// when serializing responses, AliasAs will not work -- only JsonProperty will.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AliasAsCannotBeUsedToAliasFieldNamesInResponses()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(AliasTestUrl),
                Reply.Json("{\"FIELD_WE_SHOULD_SHORTEN_WITH_ALIAS_AS\": \"Hello\", \"FIELD_WE_SHOULD_SHORTEN_WITH_JSON_PROPERTY\": \"World\"}")
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        var result = await fixture.GetTestObject();

        await Assert.That(result!.ShortNameForAlias).IsNull();
    }

    /// <summary>Verifies that a ValidationException is thrown for a Bad Request in terms of RFC 7807.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ThrowsValidationException()
    {
        var expectedContent = new ProblemDetails
        {
            Detail = DetailValue,
            Errors =
            {
                { FieldOneKey, _field1Problems },
                { FieldTwoKey, _field2Problems }
            },
            Instance = InstanceValue,
            Status = 1,
            Title = TitleValue,
            Type = "type"
        };
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(JsonSerializer.Serialize(expectedContent))
        };
        expectedResponse.Content.Headers.ContentType =
            new(ProblemJsonMediaType);
        var handler = new StubHttp
        {
            {
                Route.Get(AliasTestUrl),
                Reply.From(req => expectedResponse)
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        var actualException = await Assert.That(fixture.GetTestObject).ThrowsExactly<ValidationApiException>();
        await Assert.That(actualException!.Content).IsNotNull();
        await Assert.That(actualException.Content!.Detail).IsEqualTo(DetailValue);
        await Assert.That(actualException.Content.Errors[FieldOneKey][0]).IsEqualTo("Problem1");
        await Assert.That(actualException.Content.Errors[FieldTwoKey][0]).IsEqualTo("Problem2");
        await Assert.That(actualException.Content.Instance).IsEqualTo(InstanceValue);
        await Assert.That(actualException.Content.Status).IsEqualTo(1);
        await Assert.That(actualException.Content.Title).IsEqualTo(TitleValue);
        await Assert.That(actualException.Content.Type).IsEqualTo("type");
    }

    /// <summary>
    /// #1945: ValidationApiException should expose the ContentHeaders of the
    /// originating ApiException rather than leaving them null.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValidationApiExceptionPropagatesContentHeaders()
    {
        var expectedContent = new ProblemDetails { Detail = DetailValue, Title = TitleValue };
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(JsonSerializer.Serialize(expectedContent))
        };
        expectedResponse.Content.Headers.ContentType = new(
            ProblemJsonMediaType);
        var handler = new StubHttp
        {
            {
                Route.Get(AliasTestUrl),
                Reply.From(req => expectedResponse)
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        var actualException = await Assert.That(fixture.GetTestObject).ThrowsExactly<ValidationApiException>();

        await Assert.That(actualException!.ContentHeaders).IsNotNull();
        await Assert.That(actualException.ContentHeaders!.ContentType?.MediaType).IsEqualTo(
            ProblemJsonMediaType);
    }

    /// <summary>
    /// #1197: ValidationApiException must deserialize ProblemDetails with the configured
    /// IHttpContentSerializer instead of a hardcoded System.Text.Json instance. A
    /// case-sensitive serializer is used here, so the camelCase DetailValue key must not map
    /// to the PascalCase Detail property the way the old camelCase/case-insensitive
    /// hardcoded options would have.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValidationApiExceptionUsesConfiguredContentSerializer()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"Title\":\"mapped\",\"detail\":\"unmapped\"}")
        };
        expectedResponse.Content.Headers.ContentType = new(
            ProblemJsonMediaType);

        var localHandler = new StubHttp
        {
            {
                Route.Get(AliasTestUrl),
                Reply.From(req => expectedResponse)
            },
        };
        var localFixture = localHandler.CreateClient<IMyAliasService>(BaseAddress, new RefitSettings(
            new SystemTextJsonContentSerializer(
                new() { PropertyNameCaseInsensitive = false })));

        var actualException = await Assert.That(localFixture.GetTestObject).ThrowsExactly<ValidationApiException>();

        await Assert.That(actualException!.Content).IsNotNull();
        await Assert.That(actualException.Content!.Title).IsEqualTo("mapped");
        await Assert.That(actualException.Content.Detail).IsNull();
    }

    /// <summary>Verifies that EnsureSuccessStatusCodeAsync throws a ValidationApiException for a Bad Request in terms of RFC 7807.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task When_BadRequest_EnsureSuccessStatusCodeAsync_ThrowsValidationException()
    {
        var expectedContent = new ProblemDetails
        {
            Detail = DetailValue,
            Errors =
            {
                { FieldOneKey, _field1Problems },
                { FieldTwoKey, _field2Problems }
            },
            Instance = InstanceValue,
            Status = 1,
            Title = TitleValue,
            Type = "type"
        };

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(JsonSerializer.Serialize(expectedContent))
        };

        expectedResponse.Content.Headers.ContentType =
            new(ProblemJsonMediaType);
        var handler = new StubHttp
        {
            {
                Route.Get(GetApiResponseTestObjectUrl),
                Reply.From(req => expectedResponse)
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        using var response = await fixture.GetApiResponseTestObject();
        var actualException = await Assert.That(async () => await response!.EnsureSuccessStatusCodeAsync()).ThrowsExactly<ValidationApiException>();

        await Assert.That(actualException!.Content).IsNotNull();
        await Assert.That(actualException.Content!.Detail).IsEqualTo(DetailValue);
        await Assert.That(actualException.Content.Errors[FieldOneKey][0]).IsEqualTo("Problem1");
        await Assert.That(actualException.Content.Errors[FieldTwoKey][0]).IsEqualTo("Problem2");
        await Assert.That(actualException.Content.Instance).IsEqualTo(InstanceValue);
        await Assert.That(actualException.Content.Status).IsEqualTo(1);
        await Assert.That(actualException.Content.Title).IsEqualTo(TitleValue);
        await Assert.That(actualException.Content.Type).IsEqualTo("type");
    }

    /// <summary>Verifies that IsSuccessful returns false on a success status code when there is a deserialization error.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task When_SerializationErrorOnSuccessStatusCode_IsSuccessful_ShouldReturnFalse()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(InvalidJsonContent)
        };

        var handler = new StubHttp
        {
            {
                Route.Get(GetApiResponseTestObjectUrl),
                Reply.From(req => expectedResponse)
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        using var response = await fixture.GetApiResponseTestObject();

        await Assert.That(response!.IsReceived).IsTrue();
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(response.IsSuccessful).IsFalse();
        await Assert.That(response.Error).IsNotNull();
    }

    /// <summary>Verifies that EnsureSuccessStatusCodeAsync does not throw an ApiException on a success status code when there is a deserialization error.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task When_SerializationErrorOnSuccessStatusCode_EnsureSuccesStatusCodeAsync_DoNotThrowApiException()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(InvalidJsonContent)
        };

        var handler = new StubHttp
        {
            {
                Route.Get(GetApiResponseTestObjectUrl),
                Reply.From(req => expectedResponse)
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        using var response = await fixture.GetApiResponseTestObject();
        await response!.EnsureSuccessStatusCodeAsync();

        await Assert.That(response.IsReceived).IsTrue();
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(response.IsSuccessful).IsFalse();
        await Assert.That(response.Error).IsNotNull();
    }

    /// <summary>Verifies that EnsureSuccessfulAsync throws an ApiException on a success status code when there is a deserialization error.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task When_SerializationErrorOnSuccessStatusCode_EnsureSuccessfulAsync_ThrowsApiException()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(InvalidJsonContent)
        };

        var handler = new StubHttp
        {
            {
                Route.Get(GetApiResponseTestObjectUrl),
                Reply.From(req => expectedResponse)
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        using var response = await fixture.GetApiResponseTestObject();
        var actualException = await Assert.That(async () => await response!.EnsureSuccessfulAsync()).ThrowsExactly<ApiException>();

        await Assert.That(response!.IsReceived).IsTrue();
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(response.IsSuccessful).IsFalse();
        await Assert.That(actualException).IsNotNull();
        await Assert.That(actualException!.InnerException).IsTypeOf<JsonException>();
    }

    /// <summary>Verifies that ProblemDetails extensions are hydrated when present on the response.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WhenProblemDetailsResponseContainsExtensions_ShouldHydrateExtensions()
    {
        var expectedContent = new ProblemDetailsWithExtensions(
            Detail: DetailValue,
            Instance: InstanceValue,
            Status: 1,
            Title: TitleValue,
            Type: "type",
            Foo: "bar",
            Baz: 123.5d);

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(JsonSerializer.Serialize(expectedContent))
        };

        expectedResponse.Content.Headers.ContentType =
            new(ProblemJsonMediaType);
        var handler = new StubHttp
        {
            {
                Route.Get(AliasTestUrl),
                Reply.From(req => expectedResponse)
            },
            {
                Route.Get("http://api/soloyolo"),
                Reply.From(req => expectedResponse)
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        var actualException = await Assert.That(fixture.GetTestObject).ThrowsExactly<ValidationApiException>();
        await Assert.That(actualException!.Content).IsNotNull();
        await Assert.That(actualException.Content!.Detail).IsEqualTo(DetailValue);
        await Assert.That(actualException.Content.Instance).IsEqualTo(InstanceValue);
        await Assert.That(actualException.Content.Status).IsEqualTo(1);
        await Assert.That(actualException.Content.Title).IsEqualTo(TitleValue);
        await Assert.That(actualException.Content.Type).IsEqualTo("type");

        await Assert.That(actualException.Content.Extensions).Count().IsEqualTo(ExpectedExtensionsCount);
        var items = actualException.Content.Extensions.ToList();
        await Assert.That(items[0]).IsEqualTo(
            new(nameof(expectedContent.Foo), expectedContent.Foo));
        await Assert.That(items[1]).IsEqualTo(
            new(nameof(expectedContent.Baz), expectedContent.Baz));
    }

    /// <summary>Verifies that a non-seekable stream is handled by the System.Text.Json content serializer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WithNonSeekableStream_UsingSystemTextJsonContentSerializer()
    {
        var model = new TestAliasObject
        {
            ShortNameForAlias = nameof(WithNonSeekableStream_UsingSystemTextJsonContentSerializer),
            ShortNameForJsonProperty = nameof(TestAliasObject)
        };

        using var utf8BufferWriter = new PooledBufferWriter();

        var utf8JsonWriter = new Utf8JsonWriter(utf8BufferWriter);

        System.Text.Json.JsonSerializer.Serialize(utf8JsonWriter, model);

        await using var sourceStream = utf8BufferWriter.DetachStream();

        await using var contentStream = new ThrowOnGetLengthMemoryStream { CanGetLength = true };

        await sourceStream.CopyToAsync(contentStream);

        contentStream.Position = 0;

        contentStream.CanGetLength = false;

        var httpContent = new StreamContent(contentStream)
        {
            Headers =
            {
                ContentType = new(JsonMediaType)
                {
                    CharSet = Encoding.UTF8.WebName
                }
            }
        };

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = httpContent };

        expectedResponse.Content.Headers.ContentType = new(JsonMediaType);
        expectedResponse.StatusCode = HttpStatusCode.OK;

        var localHandler = new StubHttp
        {
            {
                Route.Get(AliasTestUrl),
                Reply.From(req => expectedResponse)
            },
        };
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());

        var localFixture = RestService.For<IMyAliasService>(BaseAddress, localHandler.ToSettings(settings));

        var result = await localFixture.GetTestObject();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ShortNameForAlias).IsEqualTo(
            nameof(WithNonSeekableStream_UsingSystemTextJsonContentSerializer));
        await Assert.That(result.ShortNameForJsonProperty).IsEqualTo(nameof(TestAliasObject));
    }

    /// <summary>Verifies that a deserialization failure over a non-seekable stream still populates ApiException.Content.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DeserializationFailureWithNonSeekableStream_PopulatesApiExceptionContent()
    {
        // Regression test for #2098: when deserialization fails, ApiException.Content
        // must still expose the raw response body. With a non-seekable/non-buffered
        // stream the serializer consumes the content, so ApiException.Create could no
        // longer re-read it and Content ended up null.
        const string rawBody = "{ this is not valid json";

        await using var contentStream = new ThrowOnGetLengthMemoryStream { CanGetLength = true };
        var bytes = "{ this is not valid json"u8.ToArray();
        await contentStream.WriteAsync(bytes);
        contentStream.Position = 0;
        contentStream.CanGetLength = false;

        var httpContent = new StreamContent(contentStream);
        httpContent.Headers.ContentType = new(JsonMediaType);

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = httpContent };

        var localHandler = new StubHttp
        {
            {
                Route.Get(AliasTestUrl),
                Reply.From(req => expectedResponse)
            },
        };
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());

        var localFixture = RestService.For<IMyAliasService>(BaseAddress, localHandler.ToSettings(settings));

        var actualException = await Assert.That(localFixture.GetTestObject).ThrowsExactly<ApiException>();

        await Assert.That(actualException!.Content).IsNotNull();
        await Assert.That(actualException.Content).IsEqualTo(rawBody);
    }

    /// <summary>Verifies that XML deserialization over a non-seekable stream does not throw a "stream already consumed" error.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task XmlDeserializationWithNonSeekableStream_DoesNotThrowStreamConsumed()
    {
        // Regression test for #1729: the XML serializer reads the body via
        // ReadAsStringAsync. The reverted #1705 probed/consumed the stream before
        // deserializing, which threw "The stream was already consumed" for XML over a
        // non-seekable network stream. Buffering via LoadIntoBufferAsync must keep this
        // scenario working.
        await using var contentStream = new ThrowOnGetLengthMemoryStream { CanGetLength = true };
        var bytes = "<XmlResponse><Identifier>abc-123</Identifier></XmlResponse>"u8.ToArray();
        await contentStream.WriteAsync(bytes);
        contentStream.Position = 0;
        contentStream.CanGetLength = false;

        var httpContent = new StreamContent(contentStream);
        httpContent.Headers.ContentType = new("application/xml");

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = httpContent };

        var localHandler = new StubHttp
        {
            {
                Route.Get("http://api/xmlTest"),
                Reply.From(req => expectedResponse)
            },
        };
        var settings = new RefitSettings(new XmlContentSerializer());

        var localFixture = RestService.For<IXmlResponseService>(BaseAddress, localHandler.ToSettings(settings));

        var result = await localFixture.GetXmlObject();

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Identifier).IsEqualTo("abc-123");
    }

    /// <summary>Verifies that a Bad Request with empty content surfaces as an ApiException.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BadRequestWithEmptyContent_ShouldReturnApiException()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(HelloWorldContent)
        };
        expectedResponse.Content.Headers.Clear();

        var handler = new StubHttp
        {
            {
                Route.Get(AliasTestUrl),
                Reply.From(req => expectedResponse)
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        var actualException = await Assert.That(fixture.GetTestObject).ThrowsExactly<ApiException>();

        await Assert.That(actualException!.Content).IsNotNull();
        await Assert.That(actualException.Content).IsEqualTo(HelloWorldContent);
    }

    /// <summary>Verifies that a Bad Request with empty content surfaces through the ApiResponse error.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BadRequestWithEmptyContent_ShouldReturnApiResponse()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(HelloWorldContent)
        };
        expectedResponse.Content.Headers.Clear();

        var handler = new StubHttp
        {
            {
                Route.Get($"{BaseAddress}/{nameof(IMyAliasService.GetApiResponseTestObject)}"),
                Reply.From(req => expectedResponse)
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        var apiResponse = await fixture.GetApiResponseTestObject();

        await Assert.That(apiResponse).IsNotNull();
        await Assert.That(apiResponse!.Error).IsNotNull();
        await Assert.That(apiResponse.HasResponseError(out var error)).IsTrue();
        await Assert.That(error!.Content).IsNotNull();
        await Assert.That(error.Content).IsEqualTo(HelloWorldContent);
    }

    /// <summary>Verifies that a Bad Request with string content surfaces through the IApiResponse error.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BadRequestWithStringContent_ShouldReturnIApiResponse()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(HelloWorldContent)
        };
        expectedResponse.Content.Headers.Clear();

        var handler = new StubHttp
        {
            {
                Route.Get($"{BaseAddress}/{nameof(IMyAliasService.GetIApiResponse)}"),
                Reply.From(req => expectedResponse)
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        var apiResponse = await fixture.GetIApiResponse();

        await Assert.That(apiResponse).IsNotNull();
        await Assert.That(apiResponse.Error).IsNotNull();
        await Assert.That(apiResponse.HasResponseError(out var error)).IsTrue();
        await Assert.That(error!.Content).IsNotNull();
        await Assert.That(error.Content).IsEqualTo(HelloWorldContent);
    }

    /// <summary>Verifies that a Bad Request with string content surfaces through the ValueTask IApiResponse error.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BadRequestWithStringContent_ShouldReturnValueTaskIApiResponse()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(HelloWorldContent)
        };
        expectedResponse.Content.Headers.Clear();

        var handler = new StubHttp
        {
            {
                Route.Get($"{BaseAddress}/{nameof(IMyAliasService.GetValueTaskIApiResponse)}"),
                Reply.From(req => expectedResponse)
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        var apiResponse = await fixture.GetValueTaskIApiResponse();

        await Assert.That(apiResponse).IsNotNull();
        await Assert.That(apiResponse.Error).IsNotNull();
        await Assert.That(apiResponse.HasResponseError(out var error)).IsTrue();
        await Assert.That(error!.Content).IsNotNull();
        await Assert.That(error.Content).IsEqualTo(HelloWorldContent);
    }

    /// <summary>Verifies that ValidationApiException hydrates the base ApiException Content.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValidationApiException_HydratesBaseContent()
    {
        var expectedProblemDetails = new ProblemDetails
        {
            Detail = DetailValue,
            Instance = InstanceValue,
            Status = 1,
            Title = TitleValue,
            Type = "type"
        };
        var expectedContent = JsonSerializer.Serialize(expectedProblemDetails);
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(expectedContent)
        };
        expectedResponse.Content.Headers.ContentType = new(
            ProblemJsonMediaType);
        var handler = new StubHttp
        {
            {
                Route.Get(AliasTestUrl),
                Reply.From(req => expectedResponse)
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        var actualException = await Assert.That(fixture.GetTestObject).ThrowsExactly<ValidationApiException>();
        var actualBaseException = actualException as ApiException;
        await Assert.That(actualBaseException!.Content).IsEqualTo(expectedContent);
    }

    /// <summary>Verifies that an HTML response on a JSON endpoint surfaces as an ApiException.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WithHtmlResponse_ShouldReturnApiException()
    {
        const string htmlResponse = "<html><body>Hello world</body></html>";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(htmlResponse)
        };
        expectedResponse.Content.Headers.Clear();

        var handler = new StubHttp
        {
            {
                Route.Get(AliasTestUrl),
                Reply.From(req => expectedResponse)
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        var actualException = await Assert.That(fixture.GetTestObject).ThrowsExactly<ApiException>();

        await Assert.That(actualException!.InnerException).IsTypeOf<JsonException>();
        await Assert.That(actualException.Content).IsNotNull();
        await Assert.That(actualException.Content).IsEqualTo(htmlResponse);
    }

    /// <summary>Verifies that an HTML response on a JSON endpoint surfaces through the ApiResponse error.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WithHtmlResponse_ShouldReturnApiResponse()
    {
        const string htmlResponse = "<html><body>Hello world</body></html>";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(htmlResponse)
        };
        expectedResponse.Content.Headers.Clear();

        var handler = new StubHttp
        {
            {
                Route.Get($"{BaseAddress}/{nameof(IMyAliasService.GetApiResponseTestObject)}"),
                Reply.From(req => expectedResponse)
            },
        };
        var fixture = RestService.For<IMyAliasService>(BaseAddress, handler.ToSettings());

        var apiResponse = await fixture.GetApiResponseTestObject();

        await Assert.That(apiResponse!.Error).IsNotNull();
        await Assert.That(apiResponse.Error!.InnerException).IsTypeOf<JsonException>();
        await Assert.That(apiResponse.HasResponseError(out var error)).IsTrue();
        await Assert.That(error!.Content).IsNotNull();
        await Assert.That(error.Content).IsEqualTo(htmlResponse);
    }

    /// <summary>Verifies that the exception factory throws a clear exception when the request message is missing.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExceptionFactory_WithoutRequestMessage_ThrowsClearException()
    {
        var settings = new RefitSettings();

        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest);

        // RequestMessage is left null, as a hand-rolled test HttpMessageHandler often does.
        var ex = await Assert.That(async () => await settings.ExceptionFactory(response)).ThrowsExactly<InvalidOperationException>();

        await Assert.That(ex!.Message).Contains("RequestMessage", StringComparison.Ordinal);
    }

    /// <summary>Problem-details payload carrying extension members for the extension-hydration test.</summary>
    /// <param name="Detail">The problem detail.</param>
    /// <param name="Instance">The problem instance.</param>
    /// <param name="Status">The status code.</param>
    /// <param name="Title">The problem title.</param>
    /// <param name="Type">The problem type.</param>
    /// <param name="Foo">An extension member serialized outside the standard ProblemDetails fields.</param>
    /// <param name="Baz">A numeric extension member serialized outside the standard ProblemDetails fields.</param>
    private sealed record ProblemDetailsWithExtensions(
        string Detail,
        string Instance,
        int Status,
        string Title,
        string Type,
        string Foo,
        double Baz);
}
