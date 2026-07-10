// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Integration tests that exercise <see cref="RestService"/> end to end against a mock HTTP handler.</summary>
public partial class RestServiceIntegrationTests
{
    /// <summary>The HTTP header name used to assert header round-tripping.</summary>
    private const string CookieHeaderName = "Cookie";

    /// <summary>The base URL for the GitHub API used across the integration tests.</summary>
    private const string GitHubBaseUrl = "https://api.github.com";

    /// <summary>The GitHub API URL for the octocat user resource.</summary>
    private const string OctocatUserUrl = "https://api.github.com/users/octocat";

    /// <summary>The GitHub API URL for the github organization members resource.</summary>
    private const string OrgMembersUrl = "https://api.github.com/orgs/github/members";

    /// <summary>The JSON payload representing a single GitHub user.</summary>
    private const string UserJson = "{ 'login':'octocat', 'avatar_url':'http://foo/bar' }";

    /// <summary>The JSON payload representing a list of GitHub organization members.</summary>
    private const string OrgMembersJson = "[{ 'login':'octocat', 'avatar_url':'http://foo/bar', 'type':'User'}]";

    /// <summary>The JSON media type used for response content.</summary>
    private const string JsonMediaType = "application/json";

    /// <summary>The login of the octocat GitHub user.</summary>
    private const string OctocatLogin = "octocat";

    /// <summary>The name of the github organization.</summary>
    private const string OrgName = "github";

    /// <summary>Verifies a GitHub user can be fetched as an API response with metadata.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubUserApiAsApiResponse()
    {
        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(
                UserJson,
                Encoding.UTF8,
                JsonMediaType),
        };
        responseMessage.Headers.Add(CookieHeaderName, "Value");

        var handler = new StubHttp
        {
            {
                Route.Get(OctocatUserUrl),
                Reply.From(req => responseMessage)
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        });

        var result = await fixture.GetUserWithMetadata(OctocatLogin);

        await Assert.That(result.Headers!.Any()).IsTrue();
        await Assert.That(result.IsSuccessStatusCode).IsTrue();
        await Assert.That(result.ReasonPhrase).IsNotNull();
        await Assert.That(result.RequestMessage).IsNotNull();
        await Assert.That(result.StatusCode == default).IsFalse();
        await Assert.That(result.Version).IsNotNull();
        await Assert.That(result.Content!.Login).IsEqualTo(OctocatLogin);
        await Assert.That(string.IsNullOrEmpty(result.Content.AvatarUrl)).IsFalse();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a 404 response surfaces correctly as an API response with metadata.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheNonExistentApiAsApiResponse()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("https://api.github.com/give-me-some-404-action"),
                Reply.Status(HttpStatusCode.NotFound)
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        });

        using var result = await fixture.NothingToSeeHereWithMetadata();
        await Assert.That(result.IsSuccessStatusCode).IsFalse();
        await Assert.That(result.ReasonPhrase).IsNotNull();
        await Assert.That(result.RequestMessage).IsNotNull();
        await Assert.That(result.StatusCode == HttpStatusCode.NotFound).IsTrue();
        await Assert.That(result.Version).IsNotNull();
        await Assert.That(result.Content).IsNull();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a 404 response throws an <see cref="ApiException"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheNonExistentApi()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("https://api.github.com/give-me-some-404-action"),
                Reply.Status(HttpStatusCode.NotFound)
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        });

        try
        {
            _ = await fixture.NothingToSeeHere();
        }
        catch (Exception ex)
        {
            await Assert.That(ex).IsTypeOf<ApiException>();
        }

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a GitHub user can be fetched as an observable API response.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubUserApiAsObservableApiResponse()
    {
        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(
                UserJson,
                Encoding.UTF8,
                JsonMediaType),
        };
        responseMessage.Headers.Add(CookieHeaderName, "Value");

        var handler = new StubHttp
        {
            {
                Route.Get(OctocatUserUrl),
                Reply.From(req => responseMessage)
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        });

        var result = await ObservableTestHelpers.AwaitWithTimeout(
            fixture.GetUserObservableWithMetadata(OctocatLogin));

        await Assert.That(result.Headers!.Any()).IsTrue();
        await Assert.That(result.IsSuccessStatusCode).IsTrue();
        await Assert.That(result.ReasonPhrase).IsNotNull();
        await Assert.That(result.RequestMessage).IsNotNull();
        await Assert.That(result.StatusCode == default).IsFalse();
        await Assert.That(result.Version).IsNotNull();
        await Assert.That(result.Content!.Login).IsEqualTo(OctocatLogin);
        await Assert.That(string.IsNullOrEmpty(result.Content.AvatarUrl)).IsFalse();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a GitHub user can be fetched as an observable <see cref="IApiResponse{T}"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubUserApiAsObservableIApiResponse()
    {
        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(
                UserJson,
                Encoding.UTF8,
                JsonMediaType),
        };
        responseMessage.Headers.Add(CookieHeaderName, "Value");

        var handler = new StubHttp
        {
            {
                Route.Get(OctocatUserUrl),
                Reply.From(req => responseMessage)
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        });

        var result = await ObservableTestHelpers.AwaitWithTimeout(
            fixture.GetUserIApiResponseObservableWithMetadata(OctocatLogin));

        await Assert.That(result.Headers!.Any()).IsTrue();
        await Assert.That(result.IsSuccessStatusCode).IsTrue();
        await Assert.That(result.ReasonPhrase).IsNotNull();
        await Assert.That(result.RequestMessage).IsNotNull();
        await Assert.That(result.StatusCode == default).IsFalse();
        await Assert.That(result.Version).IsNotNull();
        await Assert.That(result.Content!.Login).IsEqualTo(OctocatLogin);
        await Assert.That(string.IsNullOrEmpty(result.Content.AvatarUrl)).IsFalse();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a GitHub user can be fetched and deserialized.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubUserApi()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(OctocatUserUrl),
                Reply.Json(UserJson)
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        });

        var result = await fixture.GetUser(OctocatLogin);

        await Assert.That(result.Login).IsEqualTo(OctocatLogin);
        await Assert.That(string.IsNullOrEmpty(result.AvatarUrl)).IsFalse();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a camel-cased route parameter is mapped correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitWithCamelCaseParameter()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(OctocatUserUrl),
                Reply.Json(UserJson)
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        });

        var result = await ObservableTestHelpers.AwaitWithTimeout(fixture.GetUserCamelCase(OctocatLogin));

        await Assert.That(result.Login).IsEqualTo(OctocatLogin);
        await Assert.That(string.IsNullOrEmpty(result.AvatarUrl)).IsFalse();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies organization members can be fetched.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubOrgMembersApi()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(OrgMembersUrl),
                Reply.Json(OrgMembersJson)
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        });

        var result = await fixture.GetOrgMembers(OrgName);

        await Assert.That(result.Count > 0).IsTrue();
        await Assert.That(result).Contains(static member => member.Type == "User");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies organization members can be fetched concurrently.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubOrgMembersApiInParallel()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(OrgMembersUrl),
                Reply.Json(OrgMembersJson)
            },
            {
                Route.Get(OrgMembersUrl),
                Reply.Json(OrgMembersJson)
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        });

        var task1 = fixture.GetOrgMembers(OrgName);
        var task2 = fixture.GetOrgMembers(OrgName);

        await Task.WhenAll(task1, task2);

        var result1 = await task1;
        var result2 = await task2;

        await Assert.That(result1.Count > 0).IsTrue();
        await Assert.That(result1).Contains(static member => member.Type == "User");

        await Assert.That(result2.Count > 0).IsTrue();
        await Assert.That(result2).Contains(static member => member.Type == "User");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a request canceled before the response is read surfaces the cancellation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestCanceledBeforeResponseRead_WhenNoTransportExceptionFactory()
    {
        using var cts = new CancellationTokenSource();

        var handler = new StubHttp
        {
            {
                new RouteMatcher
                {
                    Method = HttpMethod.Get,
                    Template = OrgMembersUrl,
                    Reusable = true
                },
                Reply.From(req =>
                {
                    // Cancel the request
                    cts.Cancel();
                    return new(HttpStatusCode.OK)
                    {
                        Content = new StringContent(OrgMembersJson, Encoding.UTF8, JsonMediaType)
                    };
                })
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        });

        var result = await Assert.That(
            () => (Task)fixture.GetOrgMembers(OrgName, cts.Token)).ThrowsExactly<TaskCanceledException>();

        // Source generated requests directly return the SendAsync task so won't contain the caller stack frame.
        await AssertStackTraceContains(nameof(GeneratedRequestRunner.SendAsync), result!.StackTrace);
    }

    /// <summary>Verifies a request canceled before the response is read surfaces the cancellation. Providing a custom <see cref="RefitSettings.TransportExceptionFactory"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestCanceledBeforeResponseRead_WhenTransportExceptionFactory()
    {
        using var cts = new CancellationTokenSource();

        var handler = new StubHttp
        {
            {
                new RouteMatcher
                {
                    Method = HttpMethod.Get,
                    Template = OrgMembersUrl,
                    Reusable = true
                },
                Reply.From(req =>
                {
                    // Cancel the request
                    cts.Cancel();
                    return new(HttpStatusCode.OK)
                    {
                        Content = new StringContent(OrgMembersJson, Encoding.UTF8, JsonMediaType)
                    };
                })
            },
        };
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        };
        refitSettings.TransportExceptionFactory = (req, ex, _) => new ApiRequestException(req, req.Method, refitSettings, ex);
        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, refitSettings);

        var result = await Assert.That(
            () => (Task)fixture.GetOrgMembers(OrgName, cts.Token)).ThrowsExactly<ApiRequestException>();

        await Assert.That(result!.InnerException).IsNotNull();
        await Assert.That(result.InnerException).IsTypeOf<TaskCanceledException>();

        // Source generated requests directly return the SendAsync task so won't contain the caller stack frame.
        await AssertStackTraceContains(nameof(GeneratedRequestRunner.SendAsync), result.StackTrace);
    }

    /// <summary>Verifies cancellation before the response is read surfaces in the API response error.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestCanceledBeforeResponseReadWithIApiResponse_WhenNoTransportExceptionFactory()
    {
        using var cts = new CancellationTokenSource();

        var handler = new StubHttp
        {
            {
                new RouteMatcher
                {
                    Method = HttpMethod.Get,
                    Template = "https://api.github.com/users/github",
                    Reusable = true
                },
                Reply.From(req =>
                {
                    // Cancel the request
                    cts.Cancel();
                    return new(HttpStatusCode.OK)
                    {
                        Content = new StringContent(OrgMembersJson, Encoding.UTF8, JsonMediaType)
                    };
                })
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl);

        await Assert.That(
            () => (Task)fixture.GetUserWithMetadata(OrgName, cts.Token)).ThrowsExactly<TaskCanceledException>();
    }

    /// <summary>Verifies cancellation before the response is read surfaces in the API response error. Providing a custom <see cref="RefitSettings.TransportExceptionFactory"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestCanceledBeforeResponseReadWithIApiResponse_WhenTransportExceptionFactory()
    {
        using var cts = new CancellationTokenSource();

        var handler = new StubHttp
        {
            {
                new RouteMatcher
                {
                    Method = HttpMethod.Get,
                    Template = "https://api.github.com/users/github",
                    Reusable = true
                },
                Reply.From(req =>
                {
                    // Cancel the request
                    cts.Cancel();
                    return new(HttpStatusCode.OK)
                    {
                        Content = new StringContent(OrgMembersJson, Encoding.UTF8, JsonMediaType)
                    };
                })
            },
        };
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        };
        refitSettings.TransportExceptionFactory = (req, ex, _) => new ApiRequestException(req, req.Method, refitSettings, ex);

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, refitSettings);

        var result = await fixture.GetUserWithMetadata(OrgName, cts.Token);

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
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "https://api.github.com/search/users", Query = [("q", "tom repos:>42 followers:>1000")] },
                Reply.Json("{ 'total_count': 1, 'items': [{ 'login':'octocat', 'avatar_url':'http://foo/bar', 'type':'User'}]}")
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        });

        var result = await fixture.FindUsers("tom repos:>42 followers:>1000");

        await Assert.That(result.TotalCount > 0).IsTrue();
        await Assert.That(result.Items).Contains(static member => member.Type == "User");
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a GitHub user can be fetched as an observable.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubUserApiAsObservable()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(OctocatUserUrl),
                Reply.Json(UserJson)
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        });

        var result = await ObservableTestHelpers.AwaitWithTimeout(fixture.GetUserObservable(OctocatLogin));

        await Assert.That(result.Login).IsEqualTo(OctocatLogin);
        await Assert.That(string.IsNullOrEmpty(result.AvatarUrl)).IsFalse();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies an observable user request can be subscribed to after completion.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheGitHubUserApiAsObservableAndSubscribeAfterTheFact()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = OctocatUserUrl, Reusable = true },
                Reply.Json(UserJson)
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        });

        var obs = ObservableTestHelpers.WithTimeout(fixture.GetUserObservable(OctocatLogin));

        // NB: We're gonna await twice, so that the 2nd await is definitely
        // after the result has completed.
        await ObservableTestHelpers.Await(obs);
        var result2 = await ObservableTestHelpers.Await(obs);
        await Assert.That(result2.Login).IsEqualTo(OctocatLogin);
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
            ContentFactory = static () => new StringContent("test")
        };

        var client = new HttpClient(input) { BaseAddress = new("http://foo") };
        var fixture = RestService.For<IGitHubApi>(client);

        await Assert.That(input.MessagesSent).IsEqualTo(0);

        var obs = ObservableTestHelpers.WithTimeout(fixture.GetIndexObservable());

        var result1 = await ObservableTestHelpers.Await(obs);
        await Assert.That(input.MessagesSent).IsEqualTo(1);

        const int ExpectedMessagesAfterTwoSubscriptions = 2;
        var result2 = await ObservableTestHelpers.Await(obs);
        await Assert.That(input.MessagesSent).IsEqualTo(ExpectedMessagesAfterTwoSubscriptions);

        // NB: TestHttpMessageHandler returns what we tell it to ('test' by default)
        await Assert.That(result1).Contains("test");
        await Assert.That(result2).Contains("test");
    }

    /// <summary>Verifies a method returning <see cref="HttpResponseMessage"/> works.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldRetHttpResponseMessage()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "https://api.github.com/", Reusable = true },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IGitHubApi>(GitHubBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        });
        var result = await fixture.GetIndex();

        await Assert.That(result).IsNotNull();
        await Assert.That(result.IsSuccessStatusCode).IsTrue();
    }

    /// <summary>Verifies a nested interface returning <see cref="HttpResponseMessage"/> works.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ShouldRetHttpResponseMessageWithNestedInterface()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "https://api.github.com/", Reusable = true },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<TestNested.INestedGitHubApi>(GitHubBaseUrl, new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer(
                new()
                {
                    ContractResolver = new SnakeCasePropertyNamesContractResolver()
                })
        });
        var result = await fixture.GetIndex();

        await Assert.That(result).IsNotNull();
        await Assert.That(result.IsSuccessStatusCode).IsTrue();
    }
}
