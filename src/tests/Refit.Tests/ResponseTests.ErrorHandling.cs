// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Tests covering how failed and error responses surface their content and exceptions.</summary>
public sealed partial class ResponseTests
{
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
}
