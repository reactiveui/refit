// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation micro-benchmarks for the <see cref="SystemTextJsonContentSerializer"/> helpers that surround the JSON
/// engine: default options construction, JSON property-name resolution, polymorphism detection, and the reflection
/// serialize/deserialize round-trip. The source-generated fast path is covered by the request body serialization
/// benchmark; this isolates the reflection-metadata helpers.
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ContentSerializerHelperBenchmarks
{
    /// <summary>The reflection-based serializer under test.</summary>
    private readonly SystemTextJsonContentSerializer _serializer = new();

    /// <summary>The declared type inspected by the polymorphism check.</summary>
    private readonly Type _declaredType = typeof(User);

    /// <summary>A JSON object serialized and deserialized by the round-trip benchmarks.</summary>
    private User _user = null!;

    /// <summary>The reflected property whose JSON field name is resolved.</summary>
    private PropertyInfo _property = null!;

    /// <summary>The JSON string deserialized by the round-trip benchmark.</summary>
    private string _json = null!;

    /// <summary>Builds the payload, reflected property, and serialized JSON before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _user = new() { Id = 1, Name = "Ada", Bio = "mathematician", Url = "https://x/y" };
        _property = _declaredType.GetProperty(nameof(User.Name))!;
        _json = System.Text.Encoding.UTF8.GetString(_serializer.SerializeToUtf8Bytes(_user));
    }

    /// <summary>Resolves the JSON field name for a property (attribute lookup).</summary>
    /// <returns>The resolved field name length, or zero when unset.</returns>
    [Benchmark]
    [BenchmarkCategory("Metadata")]
    public int GetFieldNameForProperty() => _serializer.GetFieldNameForProperty(_property)?.Length ?? 0;

    /// <summary>Determines whether the declared type is configured for polymorphic serialization.</summary>
    /// <returns>1 when polymorphic; otherwise 0.</returns>
    [Benchmark]
    [BenchmarkCategory("Metadata")]
    public int DeclaredTypeIsPolymorphic() =>
        SystemTextJsonContentSerializer.DeclaredTypeIsPolymorphic(_declaredType, _serializer.SerializerOptions) ? 1 : 0;

    /// <summary>Serializes a JSON object to UTF-8 bytes through the reflection path.</summary>
    /// <returns>The serialized byte count.</returns>
    [Benchmark]
    [BenchmarkCategory("RoundTrip")]
    public int SerializeToUtf8Bytes() => _serializer.SerializeToUtf8Bytes(_user).Length;

    /// <summary>Deserializes a JSON string through the reflection path.</summary>
    /// <returns>The deserialized identifier.</returns>
    [Benchmark]
    [BenchmarkCategory("RoundTrip")]
    public int DeserializeFromString() => _serializer.DeserializeFromString<User>(_json)!.Id;
}
