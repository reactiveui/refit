// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using BenchmarkDotNet.Attributes;

namespace Refit.Benchmarks;

/// <summary>Benchmarks measuring Refit request dispatch performance.</summary>
[MemoryDiagnoser]
public class PerformanceBenchmark
{
    /// <summary>The base host address used for requests.</summary>
    private const string Host = "https://github.com";

    /// <summary>The sample user identifier used by the dynamic-route benchmarks.</summary>
    private const int SampleUserId = 101;

    /// <summary>The sample query values used by the complex-request benchmark.</summary>
    private static readonly int[] _sampleQueries = [1, 2, 3, 4, 5, 6];

    /// <summary>The Refit service under test.</summary>
    private IPerformanceService _service = null!;

    /// <summary>Initializes the service before the benchmarks run.</summary>
    /// <returns>A task representing the asynchronous setup.</returns>
    [GlobalSetup]
    public Task SetupAsync()
    {
        var systemTextJsonContentSerializer = new SystemTextJsonContentSerializer();
        _service =
            RestService.For<IPerformanceService>(
                Host,
                new(systemTextJsonContentSerializer)
                {
                    HttpMessageHandlerFactory = static () =>
                        new StaticValueHttpResponseHandler(
                            "Ok",
                            HttpStatusCode.OK)
                });

        return Task.CompletedTask;
    }

    /// <summary>Benchmarks a request to a constant route.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    public Task<HttpResponseMessage> ConstantRouteAsync() => _service.ConstantRouteAsync();

    /// <summary>Benchmarks a request to a route with a single dynamic segment.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    public Task<HttpResponseMessage> DynamicRouteAsync() => _service.DynamicRouteAsync(SampleUserId);

    /// <summary>Benchmarks a request to a route with multiple dynamic segments.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    public Task<HttpResponseMessage> ComplexDynamicRouteAsync() =>
        _service.ComplexDynamicRouteAsync(SampleUserId, "tom", "yCxv");

    /// <summary>Benchmarks a request that sends an object as query parameters.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    public Task<HttpResponseMessage> ObjectRequestAsync() =>
        _service.ObjectRequestAsync(new() { SomeProperty = "myProperty", SomeQuery = "myQuery" });

    /// <summary>Benchmarks a request combining dynamic routes, objects, and collections.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    public Task<HttpResponseMessage> ComplexRequestAsync() => _service.ComplexRequestAsync(
        SampleUserId,
        new() { SomeProperty = "myProperty", SomeQuery = "myQuery" },
        _sampleQueries);
}
