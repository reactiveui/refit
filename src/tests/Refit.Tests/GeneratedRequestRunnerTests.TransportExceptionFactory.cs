// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Send-path tests covering <see cref="RefitSettings.TransportExceptionFactory"/> behavior.</summary>
public partial class GeneratedRequestRunnerTests
{
    /// <summary>Verifies a cancellation the caller did not request (for example an <see cref="HttpClient"/> timeout) is still wrapped in <see cref="ApiRequestException"/>.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncWrapsCancellationWhenCallerTokenNotSignalled()
    {
        var handler = new CapturingHandler(
            (_, _) => throw new TaskCanceledException("timeout"));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

        var thrown = await Assert
            .That(
                () => GeneratedRequestRunner.SendAsync<string, string>(
                    client,
                    request,
                    CreateSettings(),
                    isApiResponse: false,
                    shouldDisposeResponse: true,
                    bufferBody: false,
                    CancellationToken.None))
            .ThrowsExactly<ApiRequestException>();

        await Assert.That(thrown!.InnerException is OperationCanceledException).IsTrue();
    }

    /// <summary>Verifies a custom <see cref="RefitSettings.TransportExceptionFactory"/> controls the exception surfaced from the send path.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SendAsyncHonorsCustomTransportExceptionFactory()
    {
        var settings = CreateSettings();
        settings.TransportExceptionFactory = (_, _, _) => new InvalidOperationException("mapped by factory");
        var handler = new CapturingHandler(
            (_, _) => throw new HttpRequestException("boom"));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);

        var thrown = await Assert
            .That(
                () => GeneratedRequestRunner.SendAsync<string, string>(
                    client,
                    request,
                    settings,
                    isApiResponse: false,
                    shouldDisposeResponse: true,
                    bufferBody: false,
                    CancellationToken.None))
            .ThrowsExactly<InvalidOperationException>();

        await Assert.That(thrown!.Message).IsEqualTo("mapped by factory");
    }
}
