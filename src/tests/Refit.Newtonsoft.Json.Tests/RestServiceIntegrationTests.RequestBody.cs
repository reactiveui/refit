// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using RichardSzalay.MockHttp;

namespace Refit.Tests;

/// <summary>Newtonsoft-specific request/response body integration tests.</summary>
public partial class RestServiceIntegrationTests
{
    /// <summary>Verifies error content can be read from error responses.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CanGetDataOutOfErrorResponses()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHttp,
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        };

        _ = mockHttp
            .When(HttpMethod.Get, "https://api.github.com/give-me-some-404-action")
            .Respond(
                HttpStatusCode.NotFound,
                "application/json",
                "{'message': 'Not Found', 'documentation_url': 'http://foo/bar'}");

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);
        try
        {
            await fixture.NothingToSeeHere();
            Assert.Fail("Expected ApiException was not thrown.");
        }
        catch (ApiException exception)
        {
            await Assert.That(exception.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
            var content = await exception.GetContentAsAsync<Dictionary<string, string>>();

            await Assert.That(content!["message"]).IsEqualTo("Not Found");
            await Assert.That(content["documentation_url"]).IsNotNull();
        }
    }

    /// <summary>Verifies error content is returned when a request fails.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ErrorsFromApiReturnErrorContent()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHttp,
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        };

        _ = mockHttp
            .Expect(HttpMethod.Post, "https://api.github.com/users")
            .Respond(
                HttpStatusCode.BadRequest,
                "application/json",
                "{ 'errors': [ 'error1', 'message' ]}");

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        var result = await Assert.That(
            () => (Task)fixture.CreateUser(new() { Name = "foo" })).ThrowsExactly<ApiException>();

        await AssertStackTraceContains(nameof(IGitHubApi.CreateUser), result!.StackTrace);

        var errors = await result.GetContentAsAsync<ErrorResponse>();

        await Assert.That(errors!.Errors).Contains("error1");
        await Assert.That(errors.Errors).Contains("message");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies error content is returned in an API response when a request fails.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ErrorsFromApiReturnErrorContentWhenApiResponse()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHttp,
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        };

        _ = mockHttp
            .Expect(HttpMethod.Post, "https://api.github.com/users")
            .Respond(
                HttpStatusCode.BadRequest,
                "application/json",
                "{ 'errors': [ 'error1', 'message' ]}");

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        using var response = await fixture.CreateUserWithMetadata(new() { Name = "foo" });
        await Assert.That(response.IsSuccessStatusCode).IsFalse();
        await Assert.That(response.Error).IsNotNull();

        await Assert.That(response.HasRequestError(out _)).IsFalse();
        await Assert.That(response.HasResponseError(out var error)).IsTrue();
        var errors = await error!.GetContentAsAsync<ErrorResponse>();

        await Assert.That(errors!.Errors).Contains("error1");
        await Assert.That(errors.Errors).Contains("message");

        mockHttp.VerifyNoOutstandingExpectation();
    }
}
