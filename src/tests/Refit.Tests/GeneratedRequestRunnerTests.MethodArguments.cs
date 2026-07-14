// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>End-to-end coverage that the source-generated request path captures the declared call arguments.</summary>
public partial class GeneratedRequestRunnerTests
{
    /// <summary>The base address used by the generated method-argument capture fixtures.</summary>
    private const string MethodArgumentsBaseUrl = "http://nowhere.com";

    /// <summary>The organization name argument shared by the generated method-argument capture fixtures.</summary>
    private const string MethodArgumentsOrgName = "dotnet";

    /// <summary>The generated path captures the declared call arguments, including the cancellation token, when enabled.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GeneratedPathCapturesMethodArgumentsWhenEnabled()
    {
        var handler = new TestHttpMessageHandler("[]");
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var fixture = RestService.For<IGitHubApi>(
            new HttpClient(handler) { BaseAddress = new(MethodArgumentsBaseUrl) },
            new RefitSettings { CaptureMethodArguments = true });

        _ = await fixture.GetOrgMembers(MethodArgumentsOrgName, token);

        await MethodArgumentCaptureAssertions.AssertCapturedAsync(handler.RequestMessage!, MethodArgumentsOrgName, token);
    }

    /// <summary>The generated path leaves the method-arguments option absent when capture is left at its default.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GeneratedPathOmitsMethodArgumentsByDefault()
    {
        var handler = new TestHttpMessageHandler("[]");
        var fixture = RestService.For<IGitHubApi>(
            new HttpClient(handler) { BaseAddress = new(MethodArgumentsBaseUrl) });

        _ = await fixture.GetOrgMembers(MethodArgumentsOrgName, CancellationToken.None);

        await MethodArgumentCaptureAssertions.AssertAbsentAsync(handler.RequestMessage!);
    }
}
