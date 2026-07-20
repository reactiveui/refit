// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Reflection.Benchmarks;

/// <summary>
/// EventPipe GC-verbose allocation profile of query-string assembly: flattening objects and dictionaries into query
/// maps, adding scalar/collection/object query parameters, encoding and parsing query strings, joining collection
/// values, building a property query key, and classifying values for flattening.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class ReflectionQueryBuildingBenchmarks
{
    /// <summary>The delimiter used to join collection query values.</summary>
    private const string CsvDelimiter = ",";

    /// <summary>The parameter index of the single mapped query parameter.</summary>
    private const int QueryParameterIndex = 0;

    /// <summary>The sample tag values flattened from the model into the query string.</summary>
    private static readonly int[] _sampleTags = [1, 2, 3, 4];

    /// <summary>The sample collection joined into a delimited query value.</summary>
    private static readonly int[] _sampleCollectionElements = [1, 2, 3, 4, 5];

    /// <summary>The request builder under test.</summary>
    private RequestBuilderImplementation _builder = null!;

    /// <summary>A populated model flattened into a query map.</summary>
    private ReflectionQueryModel _queryModel = null!;

    /// <summary>The boxed model reused across the object-flattening benchmarks.</summary>
    private object _queryModelBoxed = null!;

    /// <summary>A dictionary flattened into a query map.</summary>
    private IDictionary _dictionary = null!;

    /// <summary>The parsed scalar-query method.</summary>
    private RestMethodInfoInternal _scalarQuery = null!;

    /// <summary>The parsed collection-query method.</summary>
    private RestMethodInfoInternal _collectionQuery = null!;

    /// <summary>The parsed object-query method.</summary>
    private RestMethodInfoInternal _objectQuery = null!;

    /// <summary>The boxed scalar query value.</summary>
    private object _scalarValue = null!;

    /// <summary>The boxed collection query value.</summary>
    private object _collectionValue = null!;

    /// <summary>The query attribute declared on the collection-query parameter.</summary>
    private QueryAttribute _collectionQueryAttribute = null!;

    /// <summary>A prebuilt list of query entries encoded into a query string.</summary>
    private List<QueryParameterEntry> _queryEntries = null!;

    /// <summary>A raw query string parsed into query entries.</summary>
    private string _rawQueryString = null!;

    /// <summary>An integer collection joined into a delimited query value.</summary>
    private int[] _collectionElements = null!;

    /// <summary>The collection-query parameter's reflected information.</summary>
    private ParameterInfo _collectionParameter = null!;

    /// <summary>A property flattened into a query key.</summary>
    private PropertyInfo _nameProperty = null!;

    /// <summary>A property probed for serialization-ignore attributes.</summary>
    private PropertyInfo _idProperty = null!;

    /// <summary>A boxed simple value classified for query flattening.</summary>
    private object _simpleValue = null!;

    /// <summary>Prepares the builder, models, methods and reflected metadata before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var settings = ReflectionBenchmarkFixtures.CreateSettings();
        _builder = new(typeof(IReflectionRequestService), settings);

        _queryModel = new()
        {
            Id = ReflectionBenchmarkFixtures.SampleModelId,
            Name = "widgets & gadgets",
            Page = ReflectionBenchmarkFixtures.SamplePage,
            Tags = _sampleTags,
            Inner = new() { Code = "abc", Label = "primary" },
        };
        _queryModelBoxed = _queryModel;
        _dictionary = new Dictionary<string, string> { ["status"] = "open", ["sort"] = "desc", ["page"] = "recent" };

        _scalarQuery = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.ScalarQueryAsync), settings);
        _collectionQuery = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.CollectionQueryAsync), settings);
        _objectQuery = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.ObjectQueryAsync), settings);

        _scalarValue = "widgets";
        _collectionElements = _sampleCollectionElements;
        _collectionValue = _collectionElements;

        _collectionParameter = _collectionQuery.ParameterInfoArray[0];
        _collectionQueryAttribute = _collectionParameter.GetCustomAttribute<QueryAttribute>()!;

        _queryEntries =
        [
            new("q", "widgets and gadgets"),
            new("page", "3"),
            new("size", "25"),
            new("sort", "desc"),
        ];
        _rawQueryString = "?status=open&sort=desc&page=2";

        _nameProperty = typeof(ReflectionQueryModel).GetProperty(nameof(ReflectionQueryModel.Name))!;
        _idProperty = typeof(ReflectionQueryModel).GetProperty(nameof(ReflectionQueryModel.Id))!;
        _simpleValue = ReflectionBenchmarkFixtures.SampleUserId;
    }

    /// <summary>Flattens a populated object into query-map entries.</summary>
    /// <returns>The number of query-map entries.</returns>
    [Benchmark]
    public int BuildQueryMapObject() => _builder.BuildQueryMap(_queryModelBoxed).Count;

    /// <summary>Flattens a dictionary into query-map entries.</summary>
    /// <returns>The number of query-map entries.</returns>
    [Benchmark]
    public int BuildQueryMapDictionary() => _builder.BuildQueryMap(_dictionary).Count;

    /// <summary>Adds a scalar query parameter.</summary>
    /// <returns>The number of query entries added.</returns>
    [Benchmark]
    public int AddQueryParametersScalar()
    {
        var entries = new List<QueryParameterEntry>();
        _builder.AddQueryParameters(_scalarQuery, null, _scalarValue, entries, QueryParameterIndex, null);
        return entries.Count;
    }

    /// <summary>Adds a multi-expanded collection query parameter.</summary>
    /// <returns>The number of query entries added.</returns>
    [Benchmark]
    public int AddQueryParametersCollection()
    {
        var entries = new List<QueryParameterEntry>();
        _builder.AddQueryParameters(_collectionQuery, _collectionQueryAttribute, _collectionValue, entries, QueryParameterIndex, null);
        return entries.Count;
    }

    /// <summary>Adds an object query parameter, flattening it into the query string.</summary>
    /// <returns>The number of query entries added.</returns>
    [Benchmark]
    public int AddQueryParametersObject()
    {
        var entries = new List<QueryParameterEntry>();
        _builder.AddQueryParameters(_objectQuery, null, _queryModelBoxed, entries, QueryParameterIndex, null);
        return entries.Count;
    }

    /// <summary>Encodes the collected query entries into a query string.</summary>
    /// <returns>The encoded query string.</returns>
    [Benchmark]
    public string CreateQueryString() => RequestBuilderImplementation.CreateQueryString(_queryEntries);

    /// <summary>Parses a raw query string into query entries.</summary>
    /// <returns>The number of parsed query entries.</returns>
    [Benchmark]
    public int ParseQueryStringInto()
    {
        List<QueryParameterEntry>? entries = null;
        RequestBuilderImplementation.ParseQueryStringInto(_rawQueryString, ref entries);
        return entries?.Count ?? 0;
    }

    /// <summary>Formats and joins a collection into a delimited query value.</summary>
    /// <returns>The joined query value.</returns>
    [Benchmark]
    public string JoinFormattedQueryValues() =>
        _builder.JoinFormattedQueryValues(_collectionElements, _collectionParameter, _collectionParameter.ParameterType, CsvDelimiter);

    /// <summary>Builds the query key for a property.</summary>
    /// <returns>The property query key.</returns>
    [Benchmark]
    public string BuildPropertyQueryKey() => _builder.BuildPropertyQueryKey(_nameProperty, null, null);

    /// <summary>Classifies a simple value that is emitted directly.</summary>
    /// <returns><see langword="true"/> when the value is emitted directly.</returns>
    [Benchmark]
    public bool DoNotConvertToQueryMapSimple() => RequestBuilderImplementation.DoNotConvertToQueryMap(_simpleValue);

    /// <summary>Classifies a complex value that must be flattened, scanning its interfaces.</summary>
    /// <returns><see langword="true"/> when the value is emitted directly.</returns>
    [Benchmark]
    public bool DoNotConvertToQueryMapComplex() => RequestBuilderImplementation.DoNotConvertToQueryMap(_queryModelBoxed);

    /// <summary>Scans a property's attribute data for a serialization-ignore marker.</summary>
    /// <returns><see langword="true"/> when the property is ignored.</returns>
    [Benchmark]
    public bool ShouldIgnorePropertyInQueryMap() => RequestBuilderImplementation.ShouldIgnorePropertyInQueryMap(_idProperty);
}
