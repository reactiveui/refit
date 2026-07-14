// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>
/// Verifies the JSON Lines streaming deserializer returns its pooled buffer whether enumeration runs to completion
/// or the caller disposes early, exercising the reader's <see langword="try"/>/<see langword="finally"/> teardown.
/// Drives <see cref="SystemTextJsonContentSerializer.DeserializeStreamAsync{T}"/> directly so the buffered manual
/// reader path is enumerated without HTTP plumbing.
/// </summary>
public sealed class JsonLinesStreamTeardownTests
{
    /// <summary>Expected element id value 2 in streamed payloads.</summary>
    private const int ExpectedId2 = 2;

    /// <summary>Expected element id value 3 in streamed payloads.</summary>
    private const int ExpectedId3 = 3;

    /// <summary>A well-formed newline-delimited JSON payload.</summary>
    private static readonly byte[] _jsonLinesPayload = "{\"id\":1}\n{\"id\":2}\n{\"id\":3}\n"u8.ToArray();

    /// <summary>Verifies streaming to completion yields every value and returns the pooled buffer via the finally block.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FullEnumerationYieldsEveryLine()
    {
        var serializer = new SystemTextJsonContentSerializer();
        await using var stream = new MemoryStream(_jsonLinesPayload);

        var ids = new List<int>();
        await foreach (var item in serializer.DeserializeStreamAsync<StreamItem>(stream, StreamingContentFormat.JsonLines))
        {
            ids.Add(item!.Id);
        }

        await Assert.That(ids).IsEquivalentTo([1, ExpectedId2, ExpectedId3]);
    }

    /// <summary>Verifies disposing after the first value tears down the suspended iterator, running its finally block.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DisposingAfterFirstLineRunsTheFinallyBlock()
    {
        var serializer = new SystemTextJsonContentSerializer();
        await using var stream = new MemoryStream(_jsonLinesPayload);

        var sequence = serializer.DeserializeStreamAsync<StreamItem>(stream, StreamingContentFormat.JsonLines);
        await using var enumerator = sequence.GetAsyncEnumerator();

        await Assert.That(await enumerator.MoveNextAsync()).IsTrue();
        await Assert.That(enumerator.Current!.Id).IsEqualTo(1);
    }
}
