// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Reflection.Benchmarks;

/// <summary>
/// EventPipe GC-verbose allocation profile of header and request-property assembly: mapping a header, header
/// collection and authorization parameter; expanding a header collection; applying collected headers to a request;
/// setting a single header; and writing request options and Refit metadata.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class ReflectionHeaderAssemblyBenchmarks
{
    /// <summary>The parameter index of the single header parameter.</summary>
    private const int HeaderParameterIndex = 0;

    /// <summary>The parameter index of the header-collection parameter.</summary>
    private const int HeaderCollectionParameterIndex = 1;

    /// <summary>The parameter index of the authorization parameter.</summary>
    private const int AuthorizeParameterIndex = 2;

    /// <summary>The parsed header method.</summary>
    private RestMethodInfoInternal _headerMethod = null!;

    /// <summary>The request builder used by the request-property benchmark.</summary>
    private RequestBuilderImplementation _builder = null!;

    /// <summary>The parsed request-property method.</summary>
    private RestMethodInfoInternal _propertyMethod = null!;

    /// <summary>The boxed header-parameter value.</summary>
    private object _apiKeyValue = null!;

    /// <summary>The boxed header-collection value.</summary>
    private object _headerCollectionValue = null!;

    /// <summary>The boxed authorization-parameter value.</summary>
    private object _tokenValue = null!;

    /// <summary>A prebuilt header map applied to a request.</summary>
    private Dictionary<string, string?> _prebuiltHeaders = null!;

    /// <summary>The request-property method arguments.</summary>
    private object[] _propertyArgs = null!;

    /// <summary>The name of the header set by the single-header benchmark.</summary>
    private string _customHeaderName = null!;

    /// <summary>The value of the header set by the single-header benchmark.</summary>
    private string _customHeaderValue = null!;

    /// <summary>Prepares the parsed methods, values and header map before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var settings = ReflectionBenchmarkFixtures.CreateSettings();
        _builder = new(typeof(IReflectionRequestService), settings);
        _headerMethod = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.HeaderAsync), settings);
        _propertyMethod = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.PropertyAsync), settings);

        _apiKeyValue = "secret-key";
        _headerCollectionValue = new Dictionary<string, string> { ["X-One"] = "one", ["X-Two"] = "two", ["X-Three"] = "three" };
        _tokenValue = "jwt-token";
        _prebuiltHeaders = new() { ["X-Trace"] = "on", ["X-Region"] = "eu", ["Accept"] = "application/json" };
        _propertyArgs = [ReflectionBenchmarkFixtures.SampleUserId, ReflectionBenchmarkFixtures.SampleTraceId];
        _customHeaderName = "X-Custom";
        _customHeaderValue = "value";
    }

    /// <summary>Maps a single header parameter into the pending headers.</summary>
    /// <returns><see langword="true"/> when the parameter contributed a header.</returns>
    [Benchmark]
    public bool MapHeaderParameterSingle()
    {
        Dictionary<string, string?>? headers = null;
        return RequestBuilderImplementation.MapHeaderParameters(_headerMethod, HeaderParameterIndex, _apiKeyValue, ref headers);
    }

    /// <summary>Maps a header-collection parameter into the pending headers.</summary>
    /// <returns><see langword="true"/> when the parameter contributed a header.</returns>
    [Benchmark]
    public bool MapHeaderParameterCollection()
    {
        Dictionary<string, string?>? headers = null;
        return RequestBuilderImplementation.MapHeaderParameters(_headerMethod, HeaderCollectionParameterIndex, _headerCollectionValue, ref headers);
    }

    /// <summary>Maps an authorization parameter into the pending headers.</summary>
    /// <returns><see langword="true"/> when the parameter contributed a header.</returns>
    [Benchmark]
    public bool MapHeaderParameterAuthorize()
    {
        Dictionary<string, string?>? headers = null;
        return RequestBuilderImplementation.MapHeaderParameters(_headerMethod, AuthorizeParameterIndex, _tokenValue, ref headers);
    }

    /// <summary>Expands a header-collection argument into the pending headers.</summary>
    /// <returns>The number of headers added.</returns>
    [Benchmark]
    public int AddHeaderCollection()
    {
        Dictionary<string, string?>? headers = null;
        RequestBuilderImplementation.AddHeaderCollection(_headerCollectionValue, ref headers);
        return headers?.Count ?? 0;
    }

    /// <summary>Applies the collected headers to a request.</summary>
    /// <returns>The request the headers were applied to.</returns>
    [Benchmark]
    public HttpRequestMessage AddHeadersToRequest()
    {
        var request = new HttpRequestMessage { Method = HttpMethod.Get };
        RequestBuilderImplementation.AddHeadersToRequest(_prebuiltHeaders, request, true);
        return request;
    }

    /// <summary>Sets a single header on a request, clearing any existing value.</summary>
    /// <returns>The request the header was set on.</returns>
    [Benchmark]
    public HttpRequestMessage SetHeader()
    {
        var request = new HttpRequestMessage { Method = HttpMethod.Get };
        RequestBuilderImplementation.SetHeader(request, _customHeaderName, _customHeaderValue, true);
        return request;
    }

    /// <summary>Writes request options and Refit metadata to a request.</summary>
    /// <returns>The request the options were written to.</returns>
    [Benchmark]
    public HttpRequestMessage AddPropertiesToRequest()
    {
        var request = new HttpRequestMessage { Method = HttpMethod.Get };
        _builder.AddPropertiesToRequest(_propertyMethod, request, _propertyArgs, _propertyArgs);
        return request;
    }
}
