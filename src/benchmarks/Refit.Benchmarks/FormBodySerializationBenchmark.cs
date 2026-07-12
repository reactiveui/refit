// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>Benchmarks producing a URL-encoded form body via the reflection, generated descriptor, and unrolled paths.</summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class FormBodySerializationBenchmark
{
    /// <summary>A representative age value for the sample payload.</summary>
    private const int SampleAge = 36;

    /// <summary>The estimated form entry count used to size the unrolled buffer.</summary>
    private const int EstimatedEntryCount = 6;

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

        VerifyUnrolledMatchesDescriptor();
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

    /// <summary>Serializes the form body through the straight-line unrolled path (no descriptor array, delegates, or
    /// value boxing) exactly as an unrolled source generator would emit it.</summary>
    /// <returns>The number of bytes produced.</returns>
    [Benchmark]
    public Task<long> UnrolledAsync() => ProduceAsync(BuildUnrolled(_body));

    /// <summary>Builds the URL-encoded content straight-line, as an unrolled generator would emit for the model —
    /// direct field access, <see cref="GeneratedRequestRunner.FormatInvariant{T}"/> for value types (no boxing), and
    /// <see cref="FormUrlEncodedContent"/> for identical wire encoding.</summary>
    /// <param name="body">The payload to serialize.</param>
    /// <returns>The URL-encoded HTTP content.</returns>
    private static FormUrlEncodedContent BuildUnrolled(FormBenchmarkModel body)
    {
        var entries = new List<KeyValuePair<string?, string?>>(EstimatedEntryCount);
        if (body.FirstName is not null)
        {
            entries.Add(new("first_name", body.FirstName));
        }

        if (body.LastName is not null)
        {
            entries.Add(new("last_name", body.LastName));
        }

        if (body.Email is not null)
        {
            entries.Add(new("Email", body.Email));
        }

        entries.Add(new("Age", GeneratedRequestRunner.FormatInvariant(body.Age, null)));

        entries.Add(body.Note is not null ? new("Note", body.Note) : new("Note", string.Empty));

        foreach (var role in body.Roles)
        {
            entries.Add(new("Roles", role));
        }

        return new FormUrlEncodedContent(entries);
    }

    /// <summary>Reads the buffered content synchronously for the one-time correctness gate.</summary>
    /// <param name="content">The content to materialize.</param>
    /// <returns>The materialized string.</returns>
    private static string ReadSynchronously(HttpContent content)
    {
        using (content)
        {
            using var reader = new StreamReader(content.ReadAsStream());
            return reader.ReadToEnd();
        }
    }

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

    /// <summary>Fails setup unless the unrolled fast path produces byte-identical output to the descriptor path.</summary>
    private void VerifyUnrolledMatchesDescriptor()
    {
        var descriptor = ReadSynchronously(GeneratedRequestRunner.CreateUrlEncodedBodyContent(_settings, _body, _fields));
        var unrolled = ReadSynchronously(BuildUnrolled(_body));
        if (string.Equals(descriptor, unrolled, StringComparison.Ordinal))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Unrolled form output does not match the descriptor path.\n  descriptor: {descriptor}\n  unrolled:   {unrolled}");
    }
}
