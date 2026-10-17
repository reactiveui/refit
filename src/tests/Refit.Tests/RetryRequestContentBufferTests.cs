// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Checks every streamed write path enforces the retry body's size and cancellation limits.</summary>
public class RetryRequestContentBufferTests
{
    /// <summary>Verifies each stream write path accepts bytes up to the configured limit.</summary>
    /// <param name="mode">The stream write API.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("array")]
    [Arguments("span")]
    [Arguments("byte")]
    [Arguments("arrayAsync")]
    [Arguments("memoryAsync")]
    [Arguments("length")]
    public async Task Write_EnforcesLimit(string mode, CancellationToken cancellationToken)
    {
        await using var buffer = new RetryRequestContentBuffer(1, cancellationToken);

        await WriteAsync(buffer, mode, cancellationToken);
        await Assert.That(buffer.Length).IsEqualTo(1);
        await Assert.That(async () => await WriteAsync(buffer, mode, cancellationToken)).Throws<HttpRequestException>();
    }

    /// <summary>Verifies even synchronous writes honor cancellation of the buffering operation.</summary>
    /// <param name="mode">The stream write API.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("array")]
    [Arguments("span")]
    [Arguments("byte")]
    [Arguments("arrayAsync")]
    [Arguments("memoryAsync")]
    [Arguments("length")]
    public async Task Write_ObservesOperationCancellation(string mode)
    {
        await using var buffer = new RetryRequestContentBuffer(1, new(true));

        await Assert.That(async () => await WriteAsync(buffer, mode, CancellationToken.None)).Throws<OperationCanceledException>();
    }

    /// <summary>Verifies asynchronous write tokens are honored separately from the operation token.</summary>
    /// <param name="mode">The asynchronous stream write API.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("arrayAsync")]
    [Arguments("memoryAsync")]
    public async Task Write_ObservesWriteCancellation(string mode)
    {
        await using var buffer = new RetryRequestContentBuffer(1, CancellationToken.None);

        await Assert.That(async () => await WriteAsync(buffer, mode, new(true))).Throws<OperationCanceledException>();
    }

    /// <summary>Invokes one of the stream APIs a content serializer can use.</summary>
    /// <param name="buffer">The bounded destination.</param>
    /// <param name="mode">The stream write API.</param>
    /// <param name="cancellationToken">The write cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">The write mode is not recognized.</exception>
    private static async Task WriteAsync(RetryRequestContentBuffer buffer, string mode, CancellationToken cancellationToken)
    {
        byte[] bytes = [1];
        switch (mode)
        {
            case "array":
            {
                buffer.Write(bytes, 0, bytes.Length);
                break;
            }

            case "span":
            {
                buffer.Write(bytes.AsSpan());
                break;
            }

            case "byte":
            {
                buffer.WriteByte(1);
                break;
            }

            case "arrayAsync":
            {
                // Bind the legacy override explicitly; content serializers can still use this API.
                Func<byte[], int, int, CancellationToken, Task> writeArrayAsync = buffer.WriteAsync;
                await writeArrayAsync(bytes, 0, bytes.Length, cancellationToken);
                break;
            }

            case "memoryAsync":
            {
                await buffer.WriteAsync(bytes.AsMemory(), cancellationToken);
                break;
            }

            case "length":
            {
                buffer.SetLength(buffer.Length + 1);
                break;
            }

            default:
            {
                throw new ArgumentException("Unknown write mode.", nameof(mode));
            }
        }
    }
}
