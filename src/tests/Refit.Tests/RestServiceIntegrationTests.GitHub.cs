// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;

namespace Refit.Tests;

/// <summary>Integration tests that exercise <see cref="RestService"/> end to end against a mock HTTP handler.</summary>
public partial class RestServiceIntegrationTests
{
    /// <summary>Verifies a GitHub user can be fetched as an API response with metadata.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubUserApiAsApiResponse()
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

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(
                "{ 'login':'octocat', 'avatar_url':'http://foo/bar' }",
                Encoding.UTF8,
                "application/json"),
        };
        responseMessage.Headers.Add("Cookie", "Value");

        mockHttp
            .Expect(HttpMethod.Get, "https://api.github.com/users/octocat")
            .Respond(req => responseMessage);

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        var result = await fixture.GetUserWithMetadata("octocat");

        await Assert.That(result.Headers!.Any()).IsTrue();
        await Assert.That(result.IsSuccessStatusCode).IsTrue();
        await Assert.That(result.ReasonPhrase).IsNotNull();
        await Assert.That(result.RequestMessage).IsNotNull();
        await Assert.That(result.StatusCode == default).IsFalse();
        await Assert.That(result.Version).IsNotNull();
        await Assert.That(result.Content!.Login).IsEqualTo("octocat");
        await Assert.That(string.IsNullOrEmpty(result.Content.AvatarUrl)).IsFalse();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a 404 response surfaces correctly as an API response with metadata.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheNonExistentApiAsApiResponse()
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

        mockHttp
            .Expect(HttpMethod.Get, "https://api.github.com/give-me-some-404-action")
            .Respond(HttpStatusCode.NotFound);

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        using var result = await fixture.NothingToSeeHereWithMetadata();
        await Assert.That(result.IsSuccessStatusCode).IsFalse();
        await Assert.That(result.ReasonPhrase).IsNotNull();
        await Assert.That(result.RequestMessage).IsNotNull();
        await Assert.That(result.StatusCode == HttpStatusCode.NotFound).IsTrue();
        await Assert.That(result.Version).IsNotNull();
        await Assert.That(result.Content).IsNull();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a 404 response throws an <see cref="ApiException"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheNonExistentApi()
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

        mockHttp
            .Expect(HttpMethod.Get, "https://api.github.com/give-me-some-404-action")
            .Respond(HttpStatusCode.NotFound);

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        try
        {
            _ = await fixture.NothingToSeeHere();
        }
        catch (Exception ex)
        {
            await Assert.That(ex).IsTypeOf<ApiException>();
        }

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a GitHub user can be fetched as an observable API response.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubUserApiAsObservableApiResponse()
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

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(
                "{ 'login':'octocat', 'avatar_url':'http://foo/bar' }",
                Encoding.UTF8,
                "application/json"),
        };
        responseMessage.Headers.Add("Cookie", "Value");

        mockHttp
            .Expect(HttpMethod.Get, "https://api.github.com/users/octocat")
            .Respond(req => responseMessage);

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        var result = await fixture
            .GetUserObservableWithMetadata("octocat")
            .Timeout(TimeSpan.FromSeconds(10));

        await Assert.That(result.Headers!.Any()).IsTrue();
        await Assert.That(result.IsSuccessStatusCode).IsTrue();
        await Assert.That(result.ReasonPhrase).IsNotNull();
        await Assert.That(result.RequestMessage).IsNotNull();
        await Assert.That(result.StatusCode == default).IsFalse();
        await Assert.That(result.Version).IsNotNull();
        await Assert.That(result.Content!.Login).IsEqualTo("octocat");
        await Assert.That(string.IsNullOrEmpty(result.Content.AvatarUrl)).IsFalse();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a GitHub user can be fetched as an observable <see cref="IApiResponse{T}"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubUserApiAsObservableIApiResponse()
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

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(
                "{ 'login':'octocat', 'avatar_url':'http://foo/bar' }",
                Encoding.UTF8,
                "application/json"),
        };
        responseMessage.Headers.Add("Cookie", "Value");

        mockHttp
            .Expect(HttpMethod.Get, "https://api.github.com/users/octocat")
            .Respond(req => responseMessage);

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        var result = await fixture
            .GetUserIApiResponseObservableWithMetadata("octocat")
            .Timeout(TimeSpan.FromSeconds(10));

        await Assert.That(result.Headers!.Any()).IsTrue();
        await Assert.That(result.IsSuccessStatusCode).IsTrue();
        await Assert.That(result.ReasonPhrase).IsNotNull();
        await Assert.That(result.RequestMessage).IsNotNull();
        await Assert.That(result.StatusCode == default).IsFalse();
        await Assert.That(result.Version).IsNotNull();
        await Assert.That(result.Content!.Login).IsEqualTo("octocat");
        await Assert.That(string.IsNullOrEmpty(result.Content.AvatarUrl)).IsFalse();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a GitHub user can be fetched and deserialized.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubUserApi()
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

        mockHttp
            .Expect(HttpMethod.Get, "https://api.github.com/users/octocat")
            .Respond("application/json", "{ 'login':'octocat', 'avatar_url':'http://foo/bar' }");

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        var result = await fixture.GetUser("octocat");

        await Assert.That(result.Login).IsEqualTo("octocat");
        await Assert.That(string.IsNullOrEmpty(result.AvatarUrl)).IsFalse();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a camel-cased route parameter is mapped correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitWithCamelCaseParameter()
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

        mockHttp
            .Expect(HttpMethod.Get, "https://api.github.com/users/octocat")
            .Respond("application/json", "{ 'login':'octocat', 'avatar_url':'http://foo/bar' }");

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        var result = await fixture.GetUserCamelCase("octocat");

        await Assert.That(result.Login).IsEqualTo("octocat");
        await Assert.That(string.IsNullOrEmpty(result.AvatarUrl)).IsFalse();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies organization members can be fetched.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubOrgMembersApi()
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

        mockHttp
            .Expect(HttpMethod.Get, "https://api.github.com/orgs/github/members")
            .Respond(
                "application/json",
                "[{ 'login':'octocat', 'avatar_url':'http://foo/bar', 'type':'User'}]");

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        var result = await fixture.GetOrgMembers("github");

        await Assert.That(result.Count > 0).IsTrue();
        await Assert.That(result).Contains(member => member.Type == "User");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies organization members can be fetched concurrently.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubOrgMembersApiInParallel()
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

        mockHttp
            .Expect(HttpMethod.Get, "https://api.github.com/orgs/github/members")
            .Respond(
                "application/json",
                "[{ 'login':'octocat', 'avatar_url':'http://foo/bar', 'type':'User'}]");
        mockHttp
            .Expect(HttpMethod.Get, "https://api.github.com/orgs/github/members")
            .Respond(
                "application/json",
                "[{ 'login':'octocat', 'avatar_url':'http://foo/bar', 'type':'User'}]");

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        var task1 = fixture.GetOrgMembers("github");
        var task2 = fixture.GetOrgMembers("github");

        await Task.WhenAll(task1, task2);

        var result1 = await task1;
        var result2 = await task2;

        await Assert.That(result1.Count > 0).IsTrue();
        await Assert.That(result1).Contains(member => member.Type == "User");

        await Assert.That(result2.Count > 0).IsTrue();
        await Assert.That(result2).Contains(member => member.Type == "User");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a request canceled before the response is read surfaces the cancellation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestCanceledBeforeResponseRead()
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

        using var cts = new CancellationTokenSource();

        mockHttp
            .When(HttpMethod.Get, "https://api.github.com/orgs/github/members")
            .Respond(req =>
            {
                // Cancel the request
                cts.Cancel();

                return new(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "[{ 'login':'octocat', 'avatar_url':'http://foo/bar', 'type':'User'}]",
                        Encoding.UTF8,
                        "application/json")
                };
            });

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        var result = await Assert.That(
            () => (Task)fixture.GetOrgMembers("github", cts.Token)).ThrowsExactly<ApiRequestException>();

        await Assert.That(result!.InnerException).IsNotNull();
        await Assert.That(result.InnerException).IsTypeOf<TaskCanceledException>();
        await AssertStackTraceContains(nameof(IGitHubApi.GetOrgMembers), result.StackTrace);
    }

    /// <summary>Verifies cancellation before the response is read surfaces in the API response error.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestCanceledBeforeResponseReadWithIApiResponse()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHttp
        };

        using var cts = new CancellationTokenSource();

        mockHttp
            .When(HttpMethod.Get, "https://api.github.com/users/github")
            .Respond(req =>
            {
                // Cancel the request
                cts.Cancel();

                return new(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "[{ 'login':'octocat', 'avatar_url':'http://foo/bar', 'type':'User'}]",
                        Encoding.UTF8,
                        "application/json")
                };
            });

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        var result = await fixture.GetUserWithMetadata("github", cts.Token);

        await Assert.That(result.IsReceived).IsFalse();
        await Assert.That(result.Error).IsNotNull();
        await Assert.That(result.HasRequestError(out var error)).IsTrue();
        await Assert.That(error!.InnerException).IsTypeOf<TaskCanceledException>();
        await Assert.That(result.HasResponseError(out _)).IsFalse();
    }

    /// <summary>Verifies the GitHub user search API can be queried.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubUserSearchApi()
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

        mockHttp
            .Expect(HttpMethod.Get, "https://api.github.com/search/users")
            .WithQueryString("q", "tom repos:>42 followers:>1000")
            .Respond(
                "application/json",
                "{ 'total_count': 1, 'items': [{ 'login':'octocat', 'avatar_url':'http://foo/bar', 'type':'User'}]}");

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        var result = await fixture.FindUsers("tom repos:>42 followers:>1000");

        await Assert.That(result.TotalCount > 0).IsTrue();
        await Assert.That(result.Items).Contains(member => member.Type == "User");
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a GitHub user can be fetched as an observable.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubUserApiAsObservable()
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

        mockHttp
            .Expect(HttpMethod.Get, "https://api.github.com/users/octocat")
            .Respond("application/json", "{ 'login':'octocat', 'avatar_url':'http://foo/bar' }");

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        var result = await fixture.GetUserObservable("octocat").Timeout(TimeSpan.FromSeconds(10));

        await Assert.That(result.Login).IsEqualTo("octocat");
        await Assert.That(string.IsNullOrEmpty(result.AvatarUrl)).IsFalse();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies an observable user request can be subscribed to after completion.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubUserApiAsObservableAndSubscribeAfterTheFact()
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

        mockHttp
            .When(HttpMethod.Get, "https://api.github.com/users/octocat")
            .Respond("application/json", "{ 'login':'octocat', 'avatar_url':'http://foo/bar' }");

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

        var obs = fixture.GetUserObservable("octocat").Timeout(TimeSpan.FromSeconds(10));

        // NB: We're gonna await twice, so that the 2nd await is definitely
        // after the result has completed.
        await obs;
        var result2 = await obs;
        await Assert.That(result2.Login).IsEqualTo("octocat");
        await Assert.That(string.IsNullOrEmpty(result2.AvatarUrl)).IsFalse();
    }

    /// <summary>Verifies two subscriptions to an observable result in two requests.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TwoSubscriptionsResultInTwoRequests()
    {
        var input = new TestHttpMessageHandler
        {
            // we need to use a factory here to ensure each request gets its own httpcontent instance
            ContentFactory = () => new StringContent("test")
        };

        var client = new HttpClient(input) { BaseAddress = new("http://foo") };
        var fixture = RestService.For<IGitHubApi>(client);

        await Assert.That(input.MessagesSent).IsEqualTo(0);

        var obs = fixture.GetIndexObservable().Timeout(TimeSpan.FromSeconds(10));

        var result1 = await obs;
        await Assert.That(input.MessagesSent).IsEqualTo(1);

        var result2 = await obs;
        await Assert.That(input.MessagesSent).IsEqualTo(2);

        // NB: TestHttpMessageHandler returns what we tell it to ('test' by default)
        await Assert.That(result1).Contains("test");
        await Assert.That(result2).Contains("test");
    }

    /// <summary>Verifies a method returning <see cref="HttpResponseMessage"/> works.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldRetHttpResponseMessage()
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

        mockHttp.When(HttpMethod.Get, "https://api.github.com/").Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);
        var result = await fixture.GetIndex();

        await Assert.That(result).IsNotNull();
        await Assert.That(result.IsSuccessStatusCode).IsTrue();
    }

    /// <summary>Verifies a nested interface returning <see cref="HttpResponseMessage"/> works.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldRetHttpResponseMessageWithNestedInterface()
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

        mockHttp.When(HttpMethod.Get, "https://api.github.com/").Respond(HttpStatusCode.OK);

        var fixture = RestService.For<TestNested.INestedGitHubApi>(
            "https://api.github.com",
            settings);
        var result = await fixture.GetIndex();

        await Assert.That(result).IsNotNull();
        await Assert.That(result.IsSuccessStatusCode).IsTrue();
    }
}
