// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation micro-benchmarks for the request body content dispatch in <c>GeneratedRequestRunner.BodyContent</c>:
/// wrapping a JSON object, a raw string, and a JSON Lines sequence into <see cref="HttpContent"/>. The dispatch and
/// wrapper allocation are measured; the actual serialization happens later on copy, so it is not part of the signal.
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class RequestBodyContentBenchmarks
{
    /// <summary>The number of items in the JSON Lines payload.</summary>
    private const int ItemCount = 8;

    /// <summary>The settings supplying the content serializer.</summary>
    private readonly RefitSettings _settings = new(new SystemTextJsonContentSerializer());

    /// <summary>A raw string body.</summary>
    private readonly string _stringBody = "a raw request body payload";

    /// <summary>A JSON object body.</summary>
    private User _user = null!;

    /// <summary>A JSON Lines payload.</summary>
    private List<FastItem> _items = null!;

    /// <summary>Builds the payloads before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _user = new() { Id = 1, Name = "Ada", Bio = "mathematician", Url = "https://x/y" };
        _items = new(ItemCount);
        for (var i = 0; i < ItemCount; i++)
        {
            _items.Add(new() { Id = i, Name = "name" });
        }
    }

    /// <summary>Serializes the JSON object body straight through the content serializer, isolating the framework
    /// <see cref="HttpContent"/> construction that the dispatch benchmark also pays so the dispatch delta reads directly.</summary>
    /// <returns>The content media-type length.</returns>
    [Benchmark]
    [BenchmarkCategory("Json")]
    public int CreateJsonBodyDirect()
    {
        using var content = _settings.ContentSerializer.ToHttpContent(_user);
        return MediaTypeLength(content);
    }

    /// <summary>Dispatches a JSON object body to serialized HTTP content.</summary>
    /// <returns>The content media-type length.</returns>
    [Benchmark]
    [BenchmarkCategory("Json")]
    public int CreateJsonBody()
    {
        using var content = GeneratedRequestRunner.CreateBodyContent(_settings, _user, BodySerializationMethod.Default, false);
        return MediaTypeLength(content);
    }

    /// <summary>Dispatches a raw string body to string HTTP content.</summary>
    /// <returns>The content media-type length.</returns>
    [Benchmark]
    [BenchmarkCategory("String")]
    public int CreateStringBody()
    {
        using var content = GeneratedRequestRunner.CreateBodyContent(_settings, _stringBody, BodySerializationMethod.Default, false);
        return MediaTypeLength(content);
    }

    /// <summary>Dispatches a sequence to JSON Lines HTTP content.</summary>
    /// <returns>The content media-type length.</returns>
    [Benchmark]
    [BenchmarkCategory("JsonLines")]
    public int CreateJsonLinesBody()
    {
        using var content = GeneratedRequestRunner.CreateJsonLinesBodyContent(_settings, _items);
        return MediaTypeLength(content);
    }

    /// <summary>Returns the length of the content's media type, or zero when none is set.</summary>
    /// <param name="content">The content to inspect.</param>
    /// <returns>The media-type length.</returns>
    private static int MediaTypeLength(HttpContent content) => content.Headers.ContentType?.MediaType?.Length ?? 0;
}
