// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Integration tests covering how <see cref="RestService"/> surfaces cancellation that occurs before the response is read.</summary>
public partial class RestServiceIntegrationTests
{
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
}
