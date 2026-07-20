// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation micro-benchmarks for the <see cref="GeneratedRequestRunner"/> path and query building helpers shared by
/// generated and reflection request construction: placeholder substitution (span, integer, and formattable overloads),
/// catch-all path round-tripping, relative URI assembly, query-key composition, invariant value formatting, absolute
/// URL validation, and collection expansion.
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class RequestPathBuildingBenchmarks
{
    /// <summary>A representative integer path/query value.</summary>
    private const int IdValue = 42;

    /// <summary>The base address host used to assemble relative URIs.</summary>
    private const string Host = "https://api.example.test/";

    /// <summary>An absolute URL validated by the absolute-URL benchmark.</summary>
    private const string AbsoluteUrl = "https://host.example.test/path/resource";

    /// <summary>A representative timestamp whose invariant form contains reserved characters.</summary>
    private readonly DateTimeOffset _timestamp = new(2026, 7, 15, 12, 30, 0, TimeSpan.Zero);

    /// <summary>A representative integer value formatted by the invariant-format benchmark.</summary>
    private readonly int _idValue = IdValue;

    /// <summary>The absolute URL validated by the absolute-URL benchmark.</summary>
    private readonly string _absoluteUrl = AbsoluteUrl;

    /// <summary>The single-placeholder query template used by the span overload.</summary>
    private readonly string _spanTemplate = "/search/{q}";

    /// <summary>The single-placeholder template used by the integer overload.</summary>
    private readonly string _intTemplate = "/users/{id}";

    /// <summary>The single-placeholder template used by the formattable overload.</summary>
    private readonly string _formattableTemplate = "/events/{at}";

    /// <summary>A catch-all path value round-tripped section by section.</summary>
    private readonly string _catchAllValue = "reports/2026/q3/summary";

    /// <summary>A scalar value substituted into the span-overload placeholder.</summary>
    private readonly string _spanValue = "widgets and gadgets";

    /// <summary>A representative collection expanded by the collection benchmarks.</summary>
    private readonly int[] _ids = [1, 2, 3, 4, 5, 6, 7, 8];

    /// <summary>Settings using the pristine default formatters.</summary>
    private RefitSettings _settings = null!;

    /// <summary>Settings using a snake_case key formatter, forcing the formatted query-key path.</summary>
    private RefitSettings _snakeSettings = null!;

    /// <summary>The HTTP client whose base address relative URIs are assembled against.</summary>
    private HttpClient _client = null!;

    /// <summary>The computed placeholder range for the span template.</summary>
    private (int StartIdx, int EndIdx) _spanRange;

    /// <summary>The computed placeholder range for the integer template.</summary>
    private (int StartIdx, int EndIdx) _intRange;

    /// <summary>The computed placeholder range for the formattable template.</summary>
    private (int StartIdx, int EndIdx) _formattableRange;

    /// <summary>Builds the settings, client, and placeholder ranges before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _settings = new(new SystemTextJsonContentSerializer());
        _snakeSettings = new(new SystemTextJsonContentSerializer())
        {
            UrlParameterKeyFormatter = new SnakeCaseUrlParameterKeyFormatter(),
        };
        _client = new() { BaseAddress = new(Host) };
        _spanRange = FindPlaceholder(_spanTemplate);
        _intRange = FindPlaceholder(_intTemplate);
        _formattableRange = FindPlaceholder(_formattableTemplate);
    }

    /// <summary>Disposes the HTTP client.</summary>
    [GlobalCleanup]
    public void Cleanup() => _client.Dispose();

    /// <summary>Substitutes a single escaped placeholder through the span overload.</summary>
    /// <returns>The built path length.</returns>
    [Benchmark]
    [BenchmarkCategory("Path")]
    public int BuildPathSpan() =>
        GeneratedRequestRunner.BuildRequestPath(_spanTemplate, false, [(_spanRange, _spanValue)]).Length;

    /// <summary>Substitutes a single integer placeholder span-formatted into the path.</summary>
    /// <returns>The built path length.</returns>
    [Benchmark]
    [BenchmarkCategory("Path")]
    public int BuildPathInt() =>
        GeneratedRequestRunner.BuildRequestPath(_intTemplate, false, _intRange, IdValue).Length;

    /// <summary>Substitutes a single formattable placeholder with in-place escaping.</summary>
    /// <returns>The built path length.</returns>
    [Benchmark]
    [BenchmarkCategory("Path")]
    public int BuildPathFormattable() =>
        GeneratedRequestRunner.BuildRequestPath(_formattableTemplate, false, _formattableRange, _timestamp, null).Length;

    /// <summary>Round-trips a catch-all path value, escaping each slash-separated section.</summary>
    /// <returns>The escaped path fragment length.</returns>
    [Benchmark]
    [BenchmarkCategory("Path")]
    public int RoundTripEscapePath() =>
        GeneratedRequestRunner.RoundTripEscapePath(_catchAllValue, _settings, typeof(string), typeof(string)).Length;

    /// <summary>Assembles a relative URI under RFC 3986 resolution.</summary>
    /// <returns>The relative URI string length.</returns>
    [Benchmark]
    [BenchmarkCategory("Uri")]
    public int BuildRelativeUriRfc() =>
        GeneratedRequestRunner.BuildRelativeUri(_client, "/users/42", UrlResolutionMode.Rfc3986).OriginalString.Length;

    /// <summary>Assembles a relative URI under legacy base-address merging.</summary>
    /// <returns>The relative URI string length.</returns>
    [Benchmark]
    [BenchmarkCategory("Uri")]
    public int BuildRelativeUriLegacy() =>
        GeneratedRequestRunner.BuildRelativeUri(_client, "/users/42", UrlResolutionMode.RefitLegacy).OriginalString.Length;

    /// <summary>Composes a query key through the pristine default (no key formatter) fast path.</summary>
    /// <returns>The composed key length.</returns>
    [Benchmark]
    [BenchmarkCategory("Key")]
    public int BuildQueryKeyDefault() => GeneratedRequestRunner.BuildQueryKey(_settings, "FirstName", null, null).Length;

    /// <summary>Composes a query key through the snake_case key formatter.</summary>
    /// <returns>The composed key length.</returns>
    [Benchmark]
    [BenchmarkCategory("Key")]
    public int BuildQueryKeyFormatted() => GeneratedRequestRunner.BuildQueryKey(_snakeSettings, "FirstName", null, null).Length;

    /// <summary>Formats an integer value with the invariant culture, without boxing.</summary>
    /// <returns>The formatted value length.</returns>
    [Benchmark]
    [BenchmarkCategory("Format")]
    public int FormatInvariantInt() => GeneratedRequestRunner.FormatInvariant(_idValue, null).Length;

    /// <summary>Formats a timestamp value with the invariant culture, without boxing.</summary>
    /// <returns>The formatted value length.</returns>
    [Benchmark]
    [BenchmarkCategory("Format")]
    public int FormatInvariantTimestamp() => GeneratedRequestRunner.FormatInvariant(_timestamp, null).Length;

    /// <summary>Validates and returns the string form of an absolute URL.</summary>
    /// <returns>The absolute URL length.</returns>
    [Benchmark]
    [BenchmarkCategory("Format")]
    public int RequireAbsoluteUrl() => GeneratedRequestRunner.RequireAbsoluteUrl(_absoluteUrl).Length;

    /// <summary>Expands a customized-formatter collection property as a joined value.</summary>
    /// <returns>The built path length.</returns>
    [Benchmark]
    [BenchmarkCategory("Collection")]
    public int AddFormattedCollectionCsv()
    {
        var builder = new GeneratedQueryStringBuilder("/search");
        GeneratedRequestRunner.AddFormattedCollectionProperty(
            ref builder,
            _settings,
            _ids,
            "ids",
            CollectionFormat.Csv,
            false,
            (typeof(int[]), typeof(int[]), typeof(int[])));
        return builder.Build().Length;
    }

    /// <summary>Expands a customized-formatter collection property as repeated pairs.</summary>
    /// <returns>The built path length.</returns>
    [Benchmark]
    [BenchmarkCategory("Collection")]
    public int AddFormattedCollectionMulti()
    {
        var builder = new GeneratedQueryStringBuilder("/search");
        GeneratedRequestRunner.AddFormattedCollectionProperty(
            ref builder,
            _settings,
            _ids,
            "ids",
            CollectionFormat.Multi,
            false,
            (typeof(int[]), typeof(int[]), typeof(int[])));
        return builder.Build().Length;
    }

    /// <summary>Locates the single <c>{name}</c> placeholder range in a template.</summary>
    /// <param name="template">The template to scan.</param>
    /// <returns>The start and one-past-end indices of the placeholder.</returns>
    private static (int StartIdx, int EndIdx) FindPlaceholder(string template) =>
        (template.IndexOf('{'), template.IndexOf('}') + 1);
}
