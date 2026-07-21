// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies the method-name and raw route-template request options are published identically by the
/// source-generated request path and the reflection request path.</summary>
public partial class RestServiceIntegrationTests
{
    /// <summary>The route template of <see cref="IGitHubApi.GetUser"/>, kept verbatim as the low-cardinality label.</summary>
    private const string GetUserRouteTemplate = "/users/{username}";

    /// <summary>The user argument used to fill the route template.</summary>
    private const string SampleUserName = "octocat";

    /// <summary>The generated request path and the reflection request path publish identical method-name and raw
    /// route-template request options, and both keep the <c>{placeholder}</c> template rather than the filled URL.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MethodNameAndRelativePathTemplateMatchAcrossGeneratedAndReflectionPaths()
    {
        // Generated path: RestService.For<T> dispatches through the source-generated client, which emits the two
        // request options as compile-time literals with no runtime reflection.
        var handler = new TestHttpMessageHandler("{}");
        using var client = HttpClientTestFactory.Create(handler, new(BaseUrl));
        var generatedFixture = RestService.For<IGitHubApi>(client);

        _ = await generatedFixture.GetUser(SampleUserName);
        var generatedRequest = handler.RequestMessage!;

        // Reflection path: the reflection request builder sets the same options from RestMethodInfo metadata.
        var reflectionFixture = new RequestBuilderImplementation<IGitHubApi>();
        var reflectionRun = reflectionFixture.RunRequest(nameof(IGitHubApi.GetUser), "{}");
        var reflectionRequest = (await reflectionRun([SampleUserName])).RequestMessage!;

        var generatedMethodName = ReadStringOption(generatedRequest, HttpRequestMessageOptions.MethodName);
        var generatedTemplate = ReadStringOption(generatedRequest, HttpRequestMessageOptions.RelativePathTemplate);
        var reflectionMethodName = ReadStringOption(reflectionRequest, HttpRequestMessageOptions.MethodName);
        var reflectionTemplate = ReadStringOption(reflectionRequest, HttpRequestMessageOptions.RelativePathTemplate);

        await Assert.That(generatedMethodName).IsEqualTo(nameof(IGitHubApi.GetUser));
        await Assert.That(generatedTemplate).IsEqualTo(GetUserRouteTemplate);

        // Parity: both request paths agree on the method name and the raw route template.
        await Assert.That(reflectionMethodName).IsEqualTo(generatedMethodName);
        await Assert.That(reflectionTemplate).IsEqualTo(generatedTemplate);

        // The template stays low-cardinality; the filled request URI resolves the placeholder to the argument value.
        await Assert.That(generatedRequest.RequestUri!.PathAndQuery).IsEqualTo($"/users/{SampleUserName}");
        await Assert.That(generatedTemplate).IsNotEqualTo(generatedRequest.RequestUri!.PathAndQuery);
    }

    /// <summary>Reads a string request option across the option (net6+) and legacy property backing stores.</summary>
    /// <param name="request">The request whose option to read.</param>
    /// <param name="key">The request option key.</param>
    /// <returns>The stored string value, or null when absent.</returns>
    private static string? ReadStringOption(HttpRequestMessage request, string key)
    {
#if NET6_0_OR_GREATER
        return request.Options.TryGetValue(new HttpRequestOptionsKey<string>(key), out var value) ? value : null;
#else
        return request.Properties.TryGetValue(key, out var value) ? value as string : null;
#endif
    }
}
