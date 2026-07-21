// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>
/// Verifies the interface-level <see cref="PathPrefixAttribute"/> prepends a shared route prefix to every method's
/// path, and that the source generator and the reflection request builder produce exactly the same URI.
/// </summary>
public class PathPrefixTests
{
    /// <summary>The base address used by every client under test; its empty path keeps the relative path intact.</summary>
    private const string BaseAddress = "http://api/";

    /// <summary>The shared prefix declared by the primary test interface.</summary>
    private const string Prefix = "/api/v2";

    /// <summary>A route template used across the slash-normalization assertions.</summary>
    private const string UsersRoute = "/users";

    /// <summary>The expected result of joining <see cref="Prefix"/> with <see cref="UsersRoute"/>.</summary>
    private const string PrefixedUsers = "/api/v2/users";

    /// <summary>The identifier substituted into a placeholder route.</summary>
    private const int UserId = 5;

    /// <summary>Verifies the prefix is prepended to a route that already carries a leading slash.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task PrefixIsPrependedToLeadingSlashRoute() =>
        AssertParityAsync<IPathPrefixApi>(
            PrefixedUsers,
            static api => api.GetUsers(),
            nameof(IPathPrefixApi.GetUsers));

    /// <summary>Verifies a route without a leading slash is joined to the prefix with exactly one slash.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task PrefixJoinsRouteWithoutLeadingSlash() =>
        AssertParityAsync<IPathPrefixApi>(
            "/api/v2/orders",
            static api => api.GetOrders(),
            nameof(IPathPrefixApi.GetOrders));

    /// <summary>Verifies a trailing slash on the prefix does not produce a double slash.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task TrailingSlashPrefixDoesNotDoubleUp() =>
        AssertParityAsync<IPathPrefixTrailingSlashApi>(
            PrefixedUsers,
            static api => api.GetUsers(),
            nameof(IPathPrefixTrailingSlashApi.GetUsers));

    /// <summary>Verifies an empty prefix leaves the route unchanged.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task EmptyPrefixIsANoOp() =>
        AssertParityAsync<IPathPrefixEmptyApi>(
            UsersRoute,
            static api => api.GetUsers(),
            nameof(IPathPrefixEmptyApi.GetUsers));

    /// <summary>Verifies a route placeholder is substituted after the prefix is applied.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task PrefixPreservesPlaceholderSubstitution() =>
        AssertParityAsync<IPathPrefixApi>(
            "/api/v2/users/5",
            static api => api.GetUser(UserId),
            nameof(IPathPrefixApi.GetUser),
            UserId);

    /// <summary>Verifies a query parameter survives the prefix join.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task PrefixPreservesQueryParameter() =>
        AssertParityAsync<IPathPrefixApi>(
            "/api/v2/search?query=widgets",
            static api => api.Search("widgets"),
            nameof(IPathPrefixApi.Search),
            "widgets");

    /// <summary>Verifies a hardcoded query string survives the prefix join.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task PrefixPreservesHardcodedQueryString() =>
        AssertParityAsync<IPathPrefixApi>(
            "/api/v2/items?active=true",
            static api => api.GetActiveItems(),
            nameof(IPathPrefixApi.GetActiveItems));

    /// <summary>Verifies the client interface's prefix applies to a method inherited from a base interface.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task DerivedPrefixAppliesToInheritedMethod() =>
        AssertParityAsync<IPathPrefixDerivedApi>(
            "/api/v2/ping",
            static api => api.Ping(),
            nameof(IPathPrefixBaseApi.Ping));

    /// <summary>Verifies a method declared directly on the derived interface carries the derived prefix.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task DerivedPrefixAppliesToOwnMethod() =>
        AssertParityAsync<IPathPrefixDerivedApi>(
            "/api/v2/own",
            static api => api.Own(),
            nameof(IPathPrefixDerivedApi.Own));

    /// <summary>Verifies a base interface used directly as its own client carries its own prefix.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public Task BaseInterfaceUsesItsOwnPrefixWhenClient() =>
        AssertParityAsync<IPathPrefixBaseApi>(
            "/root/ping",
            static api => api.Ping(),
            nameof(IPathPrefixBaseApi.Ping));

    /// <summary>Verifies the slash-normalization rules the source generator and reflection builder both apply.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CombineWithPathPrefixNormalizesSlashes()
    {
        // An empty or whitespace prefix is a no-op.
        await Assert.That(RestMethodInfoInternal.CombineWithPathPrefix(string.Empty, UsersRoute)).IsEqualTo(UsersRoute);
        await Assert.That(RestMethodInfoInternal.CombineWithPathPrefix("   ", UsersRoute)).IsEqualTo(UsersRoute);

        // A prefix made only of slashes collapses to a no-op.
        await Assert.That(RestMethodInfoInternal.CombineWithPathPrefix("/", UsersRoute)).IsEqualTo(UsersRoute);

        // Exactly one slash joins the prefix and route regardless of the slashes either side carries.
        await Assert.That(RestMethodInfoInternal.CombineWithPathPrefix(Prefix, UsersRoute)).IsEqualTo(PrefixedUsers);
        await Assert.That(RestMethodInfoInternal.CombineWithPathPrefix("/api/v2/", UsersRoute)).IsEqualTo(PrefixedUsers);
        await Assert.That(RestMethodInfoInternal.CombineWithPathPrefix(Prefix, "users")).IsEqualTo(PrefixedUsers);
        await Assert.That(RestMethodInfoInternal.CombineWithPathPrefix("api/v2", "users")).IsEqualTo("api/v2/users");

        // An empty route collapses to the trimmed prefix.
        await Assert.That(RestMethodInfoInternal.CombineWithPathPrefix("/api/v2/", "/")).IsEqualTo(Prefix);
        await Assert.That(RestMethodInfoInternal.CombineWithPathPrefix(Prefix, string.Empty)).IsEqualTo(Prefix);
    }

    /// <summary>Asserts the generated and reflection request builders produce the same expected relative URI.</summary>
    /// <typeparam name="T">The Refit interface under test.</typeparam>
    /// <param name="expectedPathAndQuery">The expected path and query.</param>
    /// <param name="generatedCall">The interface method to invoke on the generated client.</param>
    /// <param name="reflectionMethod">The method name to build a reflection request for.</param>
    /// <param name="reflectionArguments">The arguments passed to the reflection request factory.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task AssertParityAsync<T>(
        string expectedPathAndQuery,
        Func<T, Task> generatedCall,
        string reflectionMethod,
        params object[] reflectionArguments)
    {
        var generated = await GeneratedPathAndQueryAsync(generatedCall);
        var reflected = await ReflectionPathAndQueryAsync<T>(reflectionMethod, reflectionArguments);

        await Assert.That(generated).IsEqualTo(expectedPathAndQuery);
        await Assert.That(reflected).IsEqualTo(expectedPathAndQuery);
    }

    /// <summary>Sends one request through the source-generated client and returns the relative URI it produced.</summary>
    /// <typeparam name="T">The Refit interface under test.</typeparam>
    /// <param name="call">The interface method to invoke.</param>
    /// <returns>The generated request's path and query.</returns>
    private static async Task<string> GeneratedPathAndQueryAsync<T>(Func<T, Task> call)
    {
        var handler = new TestHttpMessageHandler();
        using var client = HttpClientTestFactory.Create(handler, new(BaseAddress));
        var api = RestService.ForGenerated<T>(client, new RefitSettings());

        await call(api);

        return handler.RequestMessage!.RequestUri!.PathAndQuery;
    }

    /// <summary>Builds one request through the reflection request builder and returns the relative URI it produced.</summary>
    /// <typeparam name="T">The Refit interface under test.</typeparam>
    /// <param name="methodName">The method name to build a request for.</param>
    /// <param name="arguments">The arguments passed to the request factory.</param>
    /// <returns>The reflection request's path and query.</returns>
    private static async Task<string> ReflectionPathAndQueryAsync<T>(string methodName, object[] arguments)
    {
        var output = await new RequestBuilderImplementation<T>()
            .BuildRequestFactoryForMethod(methodName)(arguments);

        return output.RequestUri!.PathAndQuery;
    }
}
