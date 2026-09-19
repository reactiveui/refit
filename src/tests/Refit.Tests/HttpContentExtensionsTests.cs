// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if !NET9_0_OR_GREATER
namespace Refit.Tests;

/// <summary>Tests cancellation and buffering through the older-framework content polyfill.</summary>
public class HttpContentExtensionsTests
{
    /// <summary>Verifies that a pre-cancelled operation does not buffer content.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task LoadIntoBufferAsync_CancelledToken_ReturnsCancelledTask()
    {
        using var content = new StringContent("payload");
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.That(() => content.LoadIntoBufferAsync(cancellationTokenSource.Token))
            .ThrowsExactly<TaskCanceledException>();
    }

    /// <summary>Verifies that buffering still preserves the payload when cancellation has not been requested.</summary>
    /// <param name="cancellationToken">The token that cancels the test.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task LoadIntoBufferAsync_ActiveToken_PreservesContent(CancellationToken cancellationToken)
    {
        const string payload = "payload";
        using var content = new StringContent(payload);

        await content.LoadIntoBufferAsync(cancellationToken);

        var buffered = await content.ReadAsStringAsync(cancellationToken);
        await Assert.That(buffered).IsEqualTo(payload);
    }
}
#endif
