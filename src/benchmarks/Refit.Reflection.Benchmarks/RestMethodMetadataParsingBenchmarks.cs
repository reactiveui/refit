// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Reflection.Benchmarks;

/// <summary>
/// EventPipe GC-verbose allocation profile of the individual reflective metadata sub-parsers used while
/// constructing a <see cref="RestMethodInfoInternal"/>: header parsing, parameter/property/query map building,
/// body/authorization/URL/cancellation-token discovery, and return-type classification.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class RestMethodMetadataParsingBenchmarks
{
    /// <summary>The mapped parameters of the header method.</summary>
    private ParameterInfo[] _headerParameters = null!;

    /// <summary>The mapped parameters of the request-property method.</summary>
    private ParameterInfo[] _propertyParameters = null!;

    /// <summary>The mapped parameters of the body method.</summary>
    private ParameterInfo[] _bodyParameters = null!;

    /// <summary>The mapped parameters of the absolute-URL method.</summary>
    private ParameterInfo[] _urlParameters = null!;

    /// <summary>The raw parameters of the cancellation-token method, including the token.</summary>
    private ParameterInfo[] _cancelableRawParameters = null!;

    /// <summary>The header method's reflected information.</summary>
    private MethodInfo _headerMethod = null!;

    /// <summary>The cancellation-token method's reflected information.</summary>
    private MethodInfo _cancelableMethod = null!;

    /// <summary>The dynamic-route method's reflected information, used to classify the return type.</summary>
    private MethodInfo _returnTypeMethod = null!;

    /// <summary>A representative multi-header attribute to parse into a header map.</summary>
    private HeadersAttribute _headersAttribute = null!;

    /// <summary>The parsed body method used for the instance body-discovery benchmarks.</summary>
    private RestMethodInfoInternal _bodyMethodInfo = null!;

    /// <summary>The parsed scalar-query method used for the query-map benchmark.</summary>
    private RestMethodInfoInternal _scalarQueryMethodInfo = null!;

    /// <summary>The parsed multipart method used for the attachment-name-map benchmark.</summary>
    private RestMethodInfoInternal _multipartMethodInfo = null!;

    /// <summary>Prepares reflected metadata and parsed methods before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var settings = ReflectionBenchmarkFixtures.CreateSettings();

        _headerParameters = ReflectionBenchmarkFixtures.MappedParameters(nameof(IReflectionRequestService.HeaderAsync));
        _propertyParameters = ReflectionBenchmarkFixtures.MappedParameters(nameof(IReflectionRequestService.PropertyAsync));
        _bodyParameters = ReflectionBenchmarkFixtures.MappedParameters(nameof(IReflectionRequestService.CreateUserAsync));
        _urlParameters = ReflectionBenchmarkFixtures.MappedParameters(nameof(IReflectionRequestService.AbsoluteUrlAsync));
        _cancelableRawParameters = ReflectionBenchmarkFixtures.Method(nameof(IReflectionRequestService.CancelableAsync)).GetParameters();

        _headerMethod = ReflectionBenchmarkFixtures.Method(nameof(IReflectionRequestService.HeaderAsync));
        _cancelableMethod = ReflectionBenchmarkFixtures.Method(nameof(IReflectionRequestService.CancelableAsync));
        _returnTypeMethod = ReflectionBenchmarkFixtures.Method(nameof(IReflectionRequestService.UserByIdAsync));

        _headersAttribute = new("Authorization: Bearer token", "X-Trace: enabled", "X-Blank");

        _bodyMethodInfo = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.CreateUserAsync), settings);
        _scalarQueryMethodInfo = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.ScalarQueryAsync), settings);
        _multipartMethodInfo = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.UploadAsync), settings);
    }

    /// <summary>Reads every parameter's request-shaping attributes in a single metadata pass.</summary>
    /// <returns>The classified attribute set per parameter, returned as an object to keep the internal type off the public benchmark surface.</returns>
    [Benchmark]
    public object BuildParameterAttributeSets() =>
        RestMethodInfoInternal.BuildParameterAttributeSets(_headerParameters);

    /// <summary>Parses the static headers declared across the interface hierarchy and the method.</summary>
    /// <returns>The parsed header map.</returns>
    [Benchmark]
    public Dictionary<string, string?> ParseHeaders() =>
        RestMethodInfoInternal.ParseHeaders(typeof(IReflectionRequestService), _headerMethod);

    /// <summary>Parses a multi-entry header attribute into a header map.</summary>
    /// <returns>The accumulated header map.</returns>
    [Benchmark]
    public Dictionary<string, string?> AddHeaders()
    {
        Dictionary<string, string?>? result = null;
        RestMethodInfoInternal.AddHeaders(_headersAttribute, ref result);
        return result!;
    }

    /// <summary>Reads the attribute sets and builds the map of parameter indexes to header names, measuring the classifier's
    /// true per-call cost including the single attribute pass a real parse performs first.</summary>
    /// <returns>The header parameter map.</returns>
    [Benchmark]
    public Dictionary<int, string> BuildHeaderParameterMap() =>
        RestMethodInfoInternal.BuildHeaderParameterMap(
            RestMethodInfoInternal.BuildParameterAttributeSets(_headerParameters));

    /// <summary>Reads the attribute sets and builds the map of parameter indexes to request property keys, measuring the
    /// classifier's true per-call cost including the single attribute pass a real parse performs first.</summary>
    /// <returns>The request property map.</returns>
    [Benchmark]
    public Dictionary<int, string> BuildRequestPropertyMap() =>
        RestMethodInfoInternal.BuildRequestPropertyMap(
            _propertyParameters,
            RestMethodInfoInternal.BuildParameterAttributeSets(_propertyParameters));

    /// <summary>Strips cancellation-token parameters, exercising the array-copy path.</summary>
    /// <returns>The parameters used for request mapping.</returns>
    [Benchmark]
    public ParameterInfo[] GetNonCancellationTokenParameters() =>
        RestMethodInfoInternal.GetNonCancellationTokenParameters(_cancelableRawParameters);

    /// <summary>Reads the attribute sets and finds the header-collection parameter index, measuring the classifier's true
    /// per-call cost including the single attribute pass a real parse performs first.</summary>
    /// <returns>The index of the header-collection parameter.</returns>
    [Benchmark]
    public int GetHeaderCollectionParameterIndex() =>
        RestMethodInfoInternal.GetHeaderCollectionParameterIndex(
            _headerParameters,
            RestMethodInfoInternal.BuildParameterAttributeSets(_headerParameters));

    /// <summary>Classifies the method's return, result, and deserialized result types.</summary>
    /// <returns>The classified return type tuple.</returns>
    [Benchmark]
    public (Type ReturnType, Type ReturnResultType, Type DeserializedResultType) DetermineReturnTypeInfo() =>
        RestMethodInfoInternal.DetermineReturnTypeInfo(_returnTypeMethod, null);

    /// <summary>Reads the attribute sets and scans the body method's parameters for an explicit body attribute, measuring
    /// the classifier's true per-call cost including the single attribute pass a real parse performs first.</summary>
    /// <returns>The body attribute match tuple.</returns>
    [Benchmark]
    public (BodyAttribute? Attribute, int Index, bool HasMultiple) FindBodyAttribute() =>
        RestMethodInfoInternal.FindBodyAttribute(
            RestMethodInfoInternal.BuildParameterAttributeSets(_bodyParameters));

    /// <summary>Reads the attribute sets and finds the body parameter for a POST method, measuring the classifier's true
    /// per-call cost including the single attribute pass a real parse performs first.</summary>
    /// <returns>The body parameter information.</returns>
    [Benchmark]
    public Tuple<BodySerializationMethod, bool, int>? FindBodyParameter() =>
        _bodyMethodInfo.FindBodyParameter(
            _bodyParameters,
            RestMethodInfoInternal.BuildParameterAttributeSets(_bodyParameters),
            false,
            HttpMethod.Post);

    /// <summary>Reads the attribute sets and finds the implicit body parameter for a POST method, measuring the classifier's
    /// true per-call cost including the single attribute pass a real parse performs first.</summary>
    /// <returns>The body parameter information.</returns>
    [Benchmark]
    public Tuple<BodySerializationMethod, bool, int>? FindImplicitBodyParameter() =>
        _bodyMethodInfo.FindImplicitBodyParameter(
            _bodyParameters,
            RestMethodInfoInternal.BuildParameterAttributeSets(_bodyParameters));

    /// <summary>Reads the attribute sets and finds the authorization parameter, measuring the classifier's true per-call
    /// cost including the single attribute pass a real parse performs first.</summary>
    /// <returns>The authorization parameter information.</returns>
    [Benchmark]
    public Tuple<string, int>? FindAuthorizationParameter() =>
        RestMethodInfoInternal.FindAuthorizationParameter(
            RestMethodInfoInternal.BuildParameterAttributeSets(_headerParameters));

    /// <summary>Finds the single cancellation-token parameter.</summary>
    /// <returns>The cancellation-token parameter.</returns>
    [Benchmark]
    public ParameterInfo? FindCancellationTokenParameter() =>
        RestMethodInfoInternal.FindCancellationTokenParameter(_cancelableMethod);

    /// <summary>Reads the attribute sets and finds the <c>[Url]</c> parameter index, measuring the classifier's true per-call
    /// cost including the single attribute pass a real parse performs first.</summary>
    /// <returns>The URL parameter index.</returns>
    [Benchmark]
    public int FindUrlParameter() =>
        RestMethodInfoInternal.FindUrlParameter(
            _urlParameters,
            RestMethodInfoInternal.BuildParameterAttributeSets(_urlParameters));

    /// <summary>Reads the attribute sets and resolves the <c>[Url]</c> parameter against an empty path template, measuring
    /// the classifier's true per-call cost including the single attribute pass a real parse performs first.</summary>
    /// <returns>The URL parameter index.</returns>
    [Benchmark]
    public int ResolveUrlParameter() =>
        RestMethodInfoInternal.ResolveUrlParameter(
            _urlParameters,
            RestMethodInfoInternal.BuildParameterAttributeSets(_urlParameters),
            string.Empty);

    /// <summary>Builds the map of parameter indexes to query-string names.</summary>
    /// <returns>The query parameter map.</returns>
    [Benchmark]
    public Dictionary<int, string> BuildQueryParameterMap() =>
        _scalarQueryMethodInfo.BuildQueryParameterMap();

    /// <summary>Builds the map of parameter indexes to multipart attachment names.</summary>
    /// <returns>The attachment name map.</returns>
    [Benchmark]
    public Dictionary<int, Tuple<string, string>> BuildAttachmentNameMap() =>
        _multipartMethodInfo.BuildAttachmentNameMap();
}
