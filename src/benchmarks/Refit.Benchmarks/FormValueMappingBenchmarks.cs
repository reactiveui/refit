// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation micro-benchmarks for the form-url-encoded mapping layer: <c>FormValueMultimap</c> construction through
/// both the reflection and source-generated descriptor paths (isolated from the <see cref="FormUrlEncodedContent"/>
/// wrapping), its collection joining helper, and <see cref="DefaultFormUrlEncodedParameterFormatter"/> value formatting.
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class FormValueMappingBenchmarks
{
    /// <summary>A representative age value for the sample payload.</summary>
    private const int SampleAge = 36;

    /// <summary>The comma delimiter used when joining a collection value.</summary>
    private const string CsvDelimiter = ",";

    /// <summary>Settings using the built-in serializer that enables the descriptor path.</summary>
    private readonly RefitSettings _settings = new(new SystemTextJsonContentSerializer());

    /// <summary>The default form value formatter under test.</summary>
    private readonly DefaultFormUrlEncodedParameterFormatter _formatter = new();

    /// <summary>A representative collection joined by the collection-joining benchmark.</summary>
    private readonly List<string> _collection = ["admin", "author", "editor", "reviewer"];

    /// <summary>The payload mapped by each mapping benchmark.</summary>
    private FormBenchmarkModel _body = null!;

    /// <summary>The compile-time field descriptors mirroring <see cref="FormBenchmarkModel"/>.</summary>
    private FormField<FormBenchmarkModel>[] _fields = null!;

    /// <summary>Builds the payload and descriptors before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _body = new()
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Age = SampleAge,
            Note = null,
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
            new(static b => b.Roles, "Roles", null, null, null, CollectionFormat.Multi, false),
        ];
    }

    /// <summary>Maps the payload to form entries through the reflection property walk.</summary>
    /// <returns>The number of mapped entries.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Mapping")]
    public int ReflectionCreate() => Count(FormValueMultimap.Create(_body, _settings));

    /// <summary>Maps the payload to form entries through the source-generated descriptors.</summary>
    /// <returns>The number of mapped entries.</returns>
    [Benchmark]
    [BenchmarkCategory("Mapping")]
    public int DescriptorCreate() => Count(FormValueMultimap.CreateFromFields(_body, _fields, _settings));

    /// <summary>Joins a collection of values with the comma delimiter through the shared join helper.</summary>
    /// <returns>The joined value length.</returns>
    [Benchmark]
    [BenchmarkCategory("Join")]
    public int JoinCsvValues() =>
        FormValueMultimap.JoinFormattedValues(_collection, CsvDelimiter, null, _settings).Length;

    /// <summary>Formats a plain string value.</summary>
    /// <returns>The formatted value length.</returns>
    [Benchmark]
    [BenchmarkCategory("ValueFormat")]
    public int FormatString() => (_formatter.Format("widgets and gadgets", null) ?? string.Empty).Length;

    /// <summary>Formats an integer value (boxed through the invariant <c>string.Format</c> path).</summary>
    /// <returns>The formatted value length.</returns>
    [Benchmark]
    [BenchmarkCategory("ValueFormat")]
    public int FormatInt() => (_formatter.Format(SampleAge, null) ?? string.Empty).Length;

    /// <summary>Formats an enum value carrying an <c>[EnumMember]</c> override (exercises the enum-member cache).</summary>
    /// <returns>The formatted value length.</returns>
    [Benchmark]
    [BenchmarkCategory("ValueFormat")]
    public int FormatEnumMember() => (_formatter.Format(QuerySort.DateDescending, null) ?? string.Empty).Length;

    /// <summary>Counts the entries produced by a form value map, forcing the enumeration.</summary>
    /// <param name="map">The form value map to count.</param>
    /// <returns>The number of entries.</returns>
    private static int Count(IEnumerable map)
    {
        var count = 0;
        foreach (var _ in map)
        {
            count++;
        }

        return count;
    }
}
