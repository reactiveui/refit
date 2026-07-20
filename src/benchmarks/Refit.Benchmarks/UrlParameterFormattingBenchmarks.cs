// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation micro-benchmarks for the URL parameter value and key formatters that turn a bound argument into the
/// text placed in a route or query string: <see cref="DefaultUrlParameterFormatter"/>, the built-in
/// <see cref="IUrlParameterKeyFormatter"/> implementations, and the shared <c>SeparatedCaseFormatter</c> behind them.
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class UrlParameterFormattingBenchmarks
{
    /// <summary>A representative integer value formatted into query strings.</summary>
    private const int SampleInteger = 1_234_567;

    /// <summary>A representative timestamp whose invariant form contains reserved characters.</summary>
    private static readonly DateTimeOffset _timestamp = new(2026, 7, 15, 12, 30, 0, TimeSpan.Zero);

    /// <summary>A representative identifier value formatted into query strings.</summary>
    private static readonly Guid _guid = new("1b4e28ba-2fa1-11d2-883f-0016d3cca427");

    /// <summary>The default URL parameter value formatter under test.</summary>
    private readonly DefaultUrlParameterFormatter _valueFormatter = new();

    /// <summary>The camelCase key formatter under test.</summary>
    private readonly CamelCaseUrlParameterKeyFormatter _camelCaseKey = new();

    /// <summary>The snake_case key formatter under test.</summary>
    private readonly SnakeCaseUrlParameterKeyFormatter _snakeCaseKey = new();

    /// <summary>The kebab-case key formatter under test.</summary>
    private readonly KebabCaseUrlParameterKeyFormatter _kebabCaseKey = new();

    /// <summary>The pass-through key formatter used as a baseline.</summary>
    private readonly DefaultUrlParameterKeyFormatter _defaultKey = new();

    /// <summary>Gets or sets a representative property name formatted by the key benchmarks.</summary>
    [Params("FirstName", "HTTPStatusCode")]
    public string Key { get; set; } = "FirstName";

    /// <summary>Formats a plain string value.</summary>
    /// <returns>The formatted value length.</returns>
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int FormatString() => (_valueFormatter.Format("widgets and gadgets", typeof(string), typeof(string)) ?? string.Empty).Length;

    /// <summary>Formats an integer value (boxed through the invariant <c>string.Format</c> path).</summary>
    /// <returns>The formatted value length.</returns>
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int FormatInt() => (_valueFormatter.Format(SampleInteger, typeof(int), typeof(int)) ?? string.Empty).Length;

    /// <summary>Formats an enum value with no <c>[EnumMember]</c> override.</summary>
    /// <returns>The formatted value length.</returns>
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int FormatEnumPlain() => (_valueFormatter.Format(QuerySort.Name, typeof(QuerySort), typeof(QuerySort)) ?? string.Empty).Length;

    /// <summary>Formats an enum value carrying an <c>[EnumMember]</c> override (exercises the enum-member cache).</summary>
    /// <returns>The formatted value length.</returns>
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int FormatEnumMember() => (_valueFormatter.Format(QuerySort.DateDescending, typeof(QuerySort), typeof(QuerySort)) ?? string.Empty).Length;

    /// <summary>Formats a <see cref="DateTimeOffset"/> value.</summary>
    /// <returns>The formatted value length.</returns>
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int FormatDateTimeOffset() => (_valueFormatter.Format(_timestamp, typeof(DateTimeOffset), typeof(DateTimeOffset)) ?? string.Empty).Length;

    /// <summary>Formats a <see cref="Guid"/> value.</summary>
    /// <returns>The formatted value length.</returns>
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int FormatGuid() => (_valueFormatter.Format(_guid, typeof(Guid), typeof(Guid)) ?? string.Empty).Length;

    /// <summary>Formats a key through the pass-through default key formatter (baseline).</summary>
    /// <returns>The formatted key length.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Key")]
    public int DefaultKey() => _defaultKey.Format(Key).Length;

    /// <summary>Formats a key into camelCase.</summary>
    /// <returns>The formatted key length.</returns>
    [Benchmark]
    [BenchmarkCategory("Key")]
    public int CamelCaseKey() => _camelCaseKey.Format(Key).Length;

    /// <summary>Formats a key into snake_case through the shared separated-case formatter.</summary>
    /// <returns>The formatted key length.</returns>
    [Benchmark]
    [BenchmarkCategory("Key")]
    public int SnakeCaseKey() => _snakeCaseKey.Format(Key).Length;

    /// <summary>Formats a key into kebab-case through the shared separated-case formatter.</summary>
    /// <returns>The formatted key length.</returns>
    [Benchmark]
    [BenchmarkCategory("Key")]
    public int KebabCaseKey() => _kebabCaseKey.Format(Key).Length;
}
