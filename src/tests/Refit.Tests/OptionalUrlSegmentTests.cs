// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Verifies optional <c>{name?}</c> URL path segments (issue #591) build identically on the reflection
/// request builder and the source-generated request path.</summary>
public sealed class OptionalUrlSegmentTests
{
    /// <summary>The base address the generated client sends requests to.</summary>
    private const string BaseUrl = "http://foo";

    /// <summary>The required device identifier used by the trailing-segment fixtures.</summary>
    private const string DeviceId = "dev";

    /// <summary>The optional notification identifier used by the trailing-segment fixtures.</summary>
    private const string NotificationId = "msg";

    /// <summary>A present single-segment value used by the interior and adjacent fixtures.</summary>
    private const string PresentValue = "x";

    /// <summary>The owner value used by the optional object-property fixtures.</summary>
    private const string RepositoryOwner = "octocat";

    /// <summary>The optional repository name used by the object-property fixtures.</summary>
    private const string RepositoryName = "hello";

    /// <summary>Verifies a present optional trailing segment is included on both request paths.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task TrailingOptionalSegmentIncludedWhenPresent() =>
        AssertParityAsync(
            "/push/dev/msg",
            (nameof(IOptionalSegmentApi.GetTrailingOptional), [DeviceId, NotificationId]),
            static api => api.GetTrailingOptional(DeviceId, NotificationId));

    /// <summary>Verifies a null optional trailing segment is dropped with its preceding slash on both request paths.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task TrailingOptionalSegmentDroppedWhenNull() =>
        AssertParityAsync(
            "/push/dev",
            (nameof(IOptionalSegmentApi.GetTrailingOptional), [DeviceId, null]),
            static api => api.GetTrailingOptional(DeviceId, null));

    /// <summary>Verifies a present interior optional segment is included on both request paths.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task InteriorOptionalSegmentIncludedWhenPresent() =>
        AssertParityAsync(
            "/a/x/b",
            (nameof(IOptionalSegmentApi.GetInteriorOptional), [PresentValue]),
            static api => api.GetInteriorOptional(PresentValue));

    /// <summary>Verifies a null interior optional segment is dropped without leaving a doubled slash on both request paths.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task InteriorOptionalSegmentDroppedWhenNull() =>
        AssertParityAsync(
            "/a/b",
            (nameof(IOptionalSegmentApi.GetInteriorOptional), [null]),
            static api => api.GetInteriorOptional(null));

    /// <summary>Verifies a null optional placeholder adjacent to a required one leaves the required value untouched.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task AdjacentOptionalSegmentDroppedWhenNull() =>
        AssertParityAsync(
            "/foo/x",
            (nameof(IOptionalSegmentApi.GetAdjacentOptional), [PresentValue, null]),
            static api => api.GetAdjacentOptional(PresentValue, null));

    /// <summary>Verifies a present optional dotted object-property segment is included on both request paths.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ObjectPropertyOptionalSegmentIncludedWhenPresent()
    {
        var repo = new Repository { Owner = RepositoryOwner, Name = RepositoryName };
        await AssertParityAsync(
            "/orgs/octocat/hello",
            (nameof(IOptionalSegmentApi.GetOptionalObjectProperty), [repo]),
            api => api.GetOptionalObjectProperty(repo));
    }

    /// <summary>Verifies a null optional dotted object-property segment is dropped with its preceding slash.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ObjectPropertyOptionalSegmentDroppedWhenNull()
    {
        var repo = new Repository { Owner = RepositoryOwner, Name = null };
        await AssertParityAsync(
            "/orgs/octocat",
            (nameof(IOptionalSegmentApi.GetOptionalObjectProperty), [repo]),
            api => api.GetOptionalObjectProperty(repo));
    }

    /// <summary>Asserts the reflection and generated request paths both produce the expected relative URL.</summary>
    /// <param name="expected">The expected path and query.</param>
    /// <param name="reflectionCall">The method name and argument list for the reflection request builder.</param>
    /// <param name="generatedCall">The equivalent call on a source-generated client.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    private static async Task AssertParityAsync(
        string expected,
        (string Method, object?[] Arguments) reflectionCall,
        Func<IOptionalSegmentApi, Task> generatedCall)
    {
        var reflection = await ReflectionPathAndQueryAsync(reflectionCall.Method, reflectionCall.Arguments);
        var generated = await GeneratedPathAndQueryAsync(generatedCall);

        await Assert.That(reflection).IsEqualTo(expected);
        await Assert.That(generated).IsEqualTo(reflection);
    }

    /// <summary>Builds a request through the reflection request builder and returns its relative path and query.</summary>
    /// <param name="method">The interface method to invoke.</param>
    /// <param name="arguments">The argument list for the call.</param>
    /// <returns>The built request's path and query.</returns>
    private static async Task<string> ReflectionPathAndQueryAsync(string method, object?[] arguments)
    {
        var builder = new RequestBuilderImplementation<IOptionalSegmentApi>();
        var factory = builder.BuildRequestFactoryForMethod(method);
        var request = await factory(arguments!);
        return new Uri(new("http://api/"), request.RequestUri!).PathAndQuery;
    }

    /// <summary>Drives a call on a source-generated client and returns the captured relative path and query.</summary>
    /// <param name="call">The API call to invoke.</param>
    /// <returns>The captured request's path and query.</returns>
    private static async Task<string> GeneratedPathAndQueryAsync(Func<IOptionalSegmentApi, Task> call)
    {
        Uri? captured = null;
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = "*", Reusable = true },
                Reply.From(request =>
                {
                    captured = request.RequestUri;
                    return new(HttpStatusCode.OK) { Content = new StringContent("Ok") };
                })
            },
        };

        var fixture = handler.CreateGeneratedClient<IOptionalSegmentApi>(BaseUrl);
        await call(fixture);
        return captured!.PathAndQuery;
    }
}
