// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;

namespace Refit.Benchmarks;

/// <summary>Benchmarks producing a URL-encoded form body via the reflection and generated descriptor paths.</summary>
[MemoryDiagnoser]
public class FormBodySerializationBenchmark
{
    /// <summary>A representative age value for the sample payload.</summary>
    private const int SampleAge = 36;

    /// <summary>Settings using the built-in serializer that enables the reflection-free descriptor path.</summary>
    private static readonly RefitSettings _settings = new(new SystemTextJsonContentSerializer());

    /// <summary>The payload serialized by each benchmark.</summary>
    private FormBenchmarkModel _body = null!;

    /// <summary>The generated field descriptors mirroring <see cref="FormBenchmarkModel"/>.</summary>
    private FormField<FormBenchmarkModel>[] _fields = null!;

    /// <summary>Builds the payload and descriptors before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _body = new FormBenchmarkModel
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Age = SampleAge,
            Note = null
        };
        _body.Roles.Add("admin");
        _body.Roles.Add("author");

        _fields =
        [
            new(static b => b.FirstName, "FirstName", "first_name", null, null, null, false),
            new(static b => b.LastName, "LastName", "last_name", null, null, null, false),
            new(static b => b.Email, "Email", null, null, null, null, false),
            new(static b => b.Age, "Age", null, null, null, null, false),
            new(static b => b.Note, "Note", null, null, null, null, true),
            new(static b => b.Roles, "Roles", null, null, null, CollectionFormat.Multi, false)
        ];
    }

    /// <summary>Serializes the form body through the reflection-based <c>FormValueMultimap</c> path.</summary>
    /// <returns>The number of bytes produced.</returns>
    [Benchmark(Baseline = true)]
    public Task<long> ReflectionAsync() =>
        ProduceAsync(GeneratedRequestRunner.CreateUrlEncodedBodyContent(_settings, _body));

    /// <summary>Serializes the form body through the generated reflection-free descriptor path.</summary>
    /// <returns>The number of bytes produced.</returns>
    [Benchmark]
    public Task<long> DescriptorAsync() =>
        ProduceAsync(GeneratedRequestRunner.CreateUrlEncodedBodyContent(_settings, _body, _fields));

    /// <summary>Serializes the content to a buffer and returns the byte count.</summary>
    /// <param name="content">The HTTP content to materialize.</param>
    /// <returns>The number of bytes produced.</returns>
    private static async Task<long> ProduceAsync(HttpContent content)
    {
        using (content)
        {
            await using var stream = new MemoryStream();
            await content.CopyToAsync(stream).ConfigureAwait(false);
            return stream.Length;
        }
    }
}
