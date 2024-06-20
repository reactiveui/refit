﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Refit;
using Refit.Buffers;
using RichardSzalay.MockHttp;
// for the code gen
using Xunit;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Refit.Tests;

public class TestAliasObject
{
    [AliasAs("FIELD_WE_SHOULD_SHORTEN_WITH_ALIAS_AS")]
    public string ShortNameForAlias { get; set; }

    [JsonProperty(PropertyName = "FIELD_WE_SHOULD_SHORTEN_WITH_JSON_PROPERTY")]
    [JsonPropertyName("FIELD_WE_SHOULD_SHORTEN_WITH_JSON_PROPERTY")]
    public string ShortNameForJsonProperty { get; set; }
}

public class ResponseTests
{
    readonly MockHttpMessageHandler mockHandler;
    readonly IMyAliasService fixture;

    public ResponseTests()
    {
        mockHandler = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHandler };

        fixture = RestService.For<IMyAliasService>("http://api", settings);
    }

    public interface IMyAliasService
    {
        [Get("/aliasTest")]
        Task<TestAliasObject> GetTestObject();

        [Get("/GetApiResponseTestObject")]
        Task<ApiResponse<TestAliasObject>> GetApiResponseTestObject();

        [Get("/GetIApiResponse")]
        Task<IApiResponse> GetIApiResponse();
    }

    [Fact]
    public async Task JsonPropertyCanBeUsedToAliasFieldNamesInResponses()
    {
        mockHandler
            .Expect(HttpMethod.Get, "http://api/aliasTest")
            .Respond(
                "application/json",
                "{\"FIELD_WE_SHOULD_SHORTEN_WITH_ALIAS_AS\": \"Hello\", \"FIELD_WE_SHOULD_SHORTEN_WITH_JSON_PROPERTY\": \"World\"}"
            );

        var result = await fixture.GetTestObject();

        Assert.Equal("World", result.ShortNameForJsonProperty);
    }

    /// <summary>
    /// Even though it may seem like AliasAs and JsonProperty are used interchangeably in some places,
    /// when serializing responses, AliasAs will not work -- only JsonProperty will.
    /// </summary>
    [Fact]
    public async Task AliasAsCannotBeUsedToAliasFieldNamesInResponses()
    {
        mockHandler
            .Expect(HttpMethod.Get, "http://api/aliasTest")
            .Respond(
                "application/json",
                "{\"FIELD_WE_SHOULD_SHORTEN_WITH_ALIAS_AS\": \"Hello\", \"FIELD_WE_SHOULD_SHORTEN_WITH_JSON_PROPERTY\": \"World\"}"
            );

        var result = await fixture.GetTestObject();

        Assert.Null(result.ShortNameForAlias);
    }

    /// <summary>
    /// Test to verify if a ValidationException is thrown for a Bad Request in terms of RFC 7807
    /// </summary>
    [Fact]
    public async Task ThrowsValidationException()
    {
        var expectedContent = new ProblemDetails
        {
            Detail = "detail",
            Errors =
            {
                { "Field1", new string[] { "Problem1" } },
                { "Field2", new string[] { "Problem2" } }
            },
            Instance = "instance",
            Status = 1,
            Title = "title",
            Type = "type"
        };
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(JsonConvert.SerializeObject(expectedContent))
        };
        expectedResponse.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json");
        mockHandler.Expect(HttpMethod.Get, "http://api/aliasTest").Respond(req => expectedResponse);

        var actualException = await Assert.ThrowsAsync<ValidationApiException>(
            () => fixture.GetTestObject()
        );
        Assert.NotNull(actualException.Content);
        Assert.Equal("detail", actualException.Content.Detail);
        Assert.Equal("Problem1", actualException.Content.Errors["Field1"][0]);
        Assert.Equal("Problem2", actualException.Content.Errors["Field2"][0]);
        Assert.Equal("instance", actualException.Content.Instance);
        Assert.Equal(1, actualException.Content.Status);
        Assert.Equal("title", actualException.Content.Title);
        Assert.Equal("type", actualException.Content.Type);
    }

    /// <summary>
    /// Test to verify if EnsureSuccessStatusCodeAsync throws a ValidationApiException for a Bad Request in terms of RFC 7807
    /// </summary>
    [Fact]
    public async Task When_BadRequest_EnsureSuccessStatusCodeAsync_ThrowsValidationException()
    {
        var expectedContent = new ProblemDetails
        {
            Detail = "detail",
            Errors =
            {
                { "Field1", new string[] { "Problem1" } },
                { "Field2", new string[] { "Problem2" } }
            },
            Instance = "instance",
            Status = 1,
            Title = "title",
            Type = "type"
        };

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(JsonConvert.SerializeObject(expectedContent))
        };

        expectedResponse.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json");
        mockHandler
            .Expect(HttpMethod.Get, "http://api/GetApiResponseTestObject")
            .Respond(req => expectedResponse);

        using var response = await fixture.GetApiResponseTestObject();
        var actualException = await Assert.ThrowsAsync<ValidationApiException>(
            () => response.EnsureSuccessStatusCodeAsync()
        );

        Assert.NotNull(actualException.Content);
        Assert.Equal("detail", actualException.Content.Detail);
        Assert.Equal("Problem1", actualException.Content.Errors["Field1"][0]);
        Assert.Equal("Problem2", actualException.Content.Errors["Field2"][0]);
        Assert.Equal("instance", actualException.Content.Instance);
        Assert.Equal(1, actualException.Content.Status);
        Assert.Equal("title", actualException.Content.Title);
        Assert.Equal("type", actualException.Content.Type);
    }

    [Fact]
    public async Task WhenProblemDetailsResponseContainsExtensions_ShouldHydrateExtensions()
    {
        var expectedContent = new
        {
            Detail = "detail",
            Instance = "instance",
            Status = 1,
            Title = "title",
            Type = "type",
            Foo = "bar",
            Baz = 123d,
        };

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(JsonConvert.SerializeObject(expectedContent))
        };

        expectedResponse.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json");
        mockHandler.Expect(HttpMethod.Get, "http://api/aliasTest").Respond(req => expectedResponse);

        mockHandler.Expect(HttpMethod.Get, "http://api/soloyolo").Respond(req => expectedResponse);

        var actualException = await Assert.ThrowsAsync<ValidationApiException>(
            () => fixture.GetTestObject()
        );
        Assert.NotNull(actualException.Content);
        Assert.Equal("detail", actualException.Content.Detail);
        Assert.Equal("instance", actualException.Content.Instance);
        Assert.Equal(1, actualException.Content.Status);
        Assert.Equal("title", actualException.Content.Title);
        Assert.Equal("type", actualException.Content.Type);

        Assert.Collection(
            actualException.Content.Extensions,
            kvp =>
                Assert.Equal(
                    new KeyValuePair<string, object>(
                        nameof(expectedContent.Foo),
                        expectedContent.Foo
                    ),
                    kvp
                ),
            kvp =>
                Assert.Equal(
                    new KeyValuePair<string, object>(
                        nameof(expectedContent.Baz),
                        expectedContent.Baz
                    ),
                    kvp
                )
        );
    }

    [Fact]
    public async Task WithNonSeekableStream_UsingSystemTextJsonContentSerializer()
    {
        var model = new TestAliasObject
        {
            ShortNameForAlias = nameof(WithNonSeekableStream_UsingSystemTextJsonContentSerializer),
            ShortNameForJsonProperty = nameof(TestAliasObject)
        };

        var localHandler = new MockHttpMessageHandler();

        var settings = new RefitSettings(new SystemTextJsonContentSerializer())
        {
            HttpMessageHandlerFactory = () => localHandler
        };

        using var utf8BufferWriter = new PooledBufferWriter();

        var utf8JsonWriter = new Utf8JsonWriter(utf8BufferWriter);

        System.Text.Json.JsonSerializer.Serialize(utf8JsonWriter, model);

        using var sourceStream = utf8BufferWriter.DetachStream();

        using var contentStream = new ThrowOnGetLengthMemoryStream { CanGetLength = true };

        sourceStream.CopyTo(contentStream);

        contentStream.Position = 0;

        contentStream.CanGetLength = false;

        var httpContent = new StreamContent(contentStream)
        {
            Headers =
            {
                ContentType = new MediaTypeHeaderValue("application/json")
                {
                    CharSet = Encoding.UTF8.WebName
                }
            }
        };

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = httpContent };

        expectedResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        expectedResponse.StatusCode = HttpStatusCode.OK;

        localHandler
            .Expect(HttpMethod.Get, "http://api/aliasTest")
            .Respond(req => expectedResponse);

        var localFixture = RestService.For<IMyAliasService>("http://api", settings);

        var result = await localFixture.GetTestObject();

        Assert.NotNull(result);
        Assert.Equal(
            nameof(WithNonSeekableStream_UsingSystemTextJsonContentSerializer),
            result.ShortNameForAlias
        );
        Assert.Equal(nameof(TestAliasObject), result.ShortNameForJsonProperty);
    }

    [Fact]
    public async Task BadRequestWithEmptyContent_ShouldReturnApiException()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Hello world")
        };
        expectedResponse.Content.Headers.Clear();

        mockHandler.Expect(HttpMethod.Get, "http://api/aliasTest").Respond(req => expectedResponse);

        var actualException = await Assert.ThrowsAsync<ApiException>(() => fixture.GetTestObject());

        Assert.NotNull(actualException.Content);
        Assert.Equal("Hello world", actualException.Content);
    }

    [Fact]
    public async Task BadRequestWithEmptyContent_ShouldReturnApiResponse()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Hello world")
        };
        expectedResponse.Content.Headers.Clear();

        mockHandler
            .Expect(HttpMethod.Get, $"http://api/{nameof(fixture.GetApiResponseTestObject)}")
            .Respond(req => expectedResponse);

        var apiResponse = await fixture.GetApiResponseTestObject();

        Assert.NotNull(apiResponse);
        Assert.NotNull(apiResponse.Error);
        Assert.NotNull(apiResponse.Error.Content);
        Assert.Equal("Hello world", apiResponse.Error.Content);
    }

    [Fact]
    public async Task BadRequestWithStringContent_ShouldReturnIApiResponse()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Hello world")
        };
        expectedResponse.Content.Headers.Clear();

        mockHandler
            .Expect(HttpMethod.Get, $"http://api/{nameof(fixture.GetIApiResponse)}")
            .Respond(req => expectedResponse);

        var apiResponse = await fixture.GetIApiResponse();

        Assert.NotNull(apiResponse);
        Assert.NotNull(apiResponse.Error);
        Assert.NotNull(apiResponse.Error.Content);
        Assert.Equal("Hello world", apiResponse.Error.Content);
    }

    [Fact]
    public async Task ValidationApiException_HydratesBaseContent()
    {
        var expectedProblemDetails = new ProblemDetails
        {
            Detail = "detail",
            Instance = "instance",
            Status = 1,
            Title = "title",
            Type = "type"
        };
        var expectedContent = JsonConvert.SerializeObject(expectedProblemDetails);
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(expectedContent)
        };
        expectedResponse.Content.Headers.ContentType = new MediaTypeHeaderValue(
            "application/problem+json"
        );
        mockHandler.Expect(HttpMethod.Get, "http://api/aliasTest").Respond(req => expectedResponse);

        var actualException = await Assert.ThrowsAsync<ValidationApiException>(
            () => fixture.GetTestObject()
        );
        var actualBaseException = actualException as ApiException;
        Assert.Equal(expectedContent, actualBaseException.Content);
    }

    [Fact]
    public async Task WithHtmlResponse_ShouldReturnApiException()
    {
        const string htmlResponse = "<html><body>Hello world</body></html>";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(htmlResponse)
        };
        expectedResponse.Content.Headers.Clear();

        mockHandler.Expect(HttpMethod.Get, "http://api/aliasTest").Respond(req => expectedResponse);

        var actualException = await Assert.ThrowsAsync<ApiException>(() => fixture.GetTestObject());

        Assert.IsType<System.Text.Json.JsonException>(actualException.InnerException);
        Assert.NotNull(actualException.Content);
        Assert.Equal(htmlResponse, actualException.Content);
    }

    [Fact]
    public async Task WithHtmlResponse_ShouldReturnApiResponse()
    {
        const string htmlResponse = "<html><body>Hello world</body></html>";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(htmlResponse)
        };
        expectedResponse.Content.Headers.Clear();

        mockHandler
            .Expect(HttpMethod.Get, $"http://api/{nameof(fixture.GetApiResponseTestObject)}")
            .Respond(req => expectedResponse);

        var apiResponse = await fixture.GetApiResponseTestObject();

        Assert.NotNull(apiResponse.Error);
        Assert.IsType<System.Text.Json.JsonException>(apiResponse.Error.InnerException);
        Assert.NotNull(apiResponse.Error.Content);
        Assert.Equal(htmlResponse, apiResponse.Error.Content);
    }

    [Fact]
    public async Task WithNonJsonResponseUsingNewtonsoftJsonContentSerializer_ShouldReturnApiException()
    {
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHandler,
            ContentSerializer = new NewtonsoftJsonContentSerializer()
        };

        var newtonSoftFixture = RestService.For<IMyAliasService>("http://api", settings);

        const string nonJsonResponse = "bad response";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(nonJsonResponse)
        };
        expectedResponse.Content.Headers.Clear();

        mockHandler.Expect(HttpMethod.Get, "http://api/aliasTest").Respond(req => expectedResponse);

        var actualException = await Assert.ThrowsAsync<ApiException>(
            () => newtonSoftFixture.GetTestObject()
        );

        Assert.IsType<JsonReaderException>(actualException.InnerException);
        Assert.NotNull(actualException.Content);
        Assert.Equal(nonJsonResponse, actualException.Content);
    }

    [Fact]
    public async Task WithNonJsonResponseUsingNewtonsoftJsonContentSerializer_ShouldReturnApiResponse()
    {
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHandler,
            ContentSerializer = new NewtonsoftJsonContentSerializer()
        };

        var newtonSoftFixture = RestService.For<IMyAliasService>("http://api", settings);

        const string nonJsonResponse = "bad response";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(nonJsonResponse)
        };
        expectedResponse.Content.Headers.Clear();

        mockHandler
            .Expect(HttpMethod.Get, $"http://api/{nameof(fixture.GetApiResponseTestObject)}")
            .Respond(req => expectedResponse);

        var apiResponse = await newtonSoftFixture.GetApiResponseTestObject();

        Assert.NotNull(apiResponse.Error);
        Assert.IsType<JsonReaderException>(apiResponse.Error.InnerException);
        Assert.NotNull(apiResponse.Error.Content);
        Assert.Equal(nonJsonResponse, apiResponse.Error.Content);
    }
}

public sealed class ThrowOnGetLengthMemoryStream : MemoryStream
{
    public bool CanGetLength { get; set; }

    public override bool CanSeek
    {
        get => CanGetLength;
    }
    public override long Length => CanGetLength ? base.Length : throw new NotSupportedException();
}
