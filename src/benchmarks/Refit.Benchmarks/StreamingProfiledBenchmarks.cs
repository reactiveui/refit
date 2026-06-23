// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>EventPipe CPU-sampling profile of Refit streaming, to show whether time is spent in Refit or in System.Text.Json.</summary>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
public class StreamingProfiledBenchmarks
{
    /// <summary>The host address used for requests.</summary>
    private const string Host = "https://github.com";

    /// <summary>The payload item count used for profiling.</summary>
    private const int ItemCount = 1000;

    /// <summary>The Refit streaming service under test.</summary>
    private IStreamingService _service = null!;

    /// <summary>Builds the payload and Refit service before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();

        var users = new List<User>(ItemCount);
        for (var i = 0; i < ItemCount; i++)
        {
            users.Add(new() { Id = i, Name = "name", Bio = "bio", Url = "url", Followers = i, Following = i });
        }

        var payload = JsonSerializer.Serialize(users, options);

        _service = RestService.For<IStreamingService>(
            Host,
            new(new SystemTextJsonContentSerializer(options))
            {
                HttpMessageHandlerFactory = () => new StaticValueHttpResponseHandler(payload, HttpStatusCode.OK),
            });
    }

    /// <summary>Streams and counts the response through Refit's IAsyncEnumerable support.</summary>
    /// <returns>The number of items read.</returns>
    [Benchmark(Baseline = true)]
    public async Task<int> RefitStreamingAsync()
    {
        var count = 0;
        await foreach (var user in _service.StreamItemsAsync().ConfigureAwait(false))
        {
            if (user is not null)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Buffers the whole response into a list through Refit.</summary>
    /// <returns>The number of items read.</returns>
    [Benchmark]
    public async Task<int> RefitBufferedAsync()
    {
        var users = await _service.GetItemsAsync().ConfigureAwait(false);
        return users.Count;
    }
}
