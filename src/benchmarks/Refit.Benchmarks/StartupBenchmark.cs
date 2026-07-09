// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using BenchmarkDotNet.Attributes;

namespace Refit.Benchmarks;

/// <summary>Benchmarks the startup cost of creating Refit services and making first calls.</summary>
[MemoryDiagnoser]
public class StartupBenchmark
{
    /// <summary>The host used for benchmark requests.</summary>
    private const string Host = "https://github.com";

    /// <summary>The Refit settings using a static response handler.</summary>
    private readonly RefitSettings _settings = new()
    {
        HttpMessageHandlerFactory = static () =>
            new StaticValueHttpResponseHandler(
                "Ok",
                HttpStatusCode.OK)
    };

    /// <summary>A pre-created service instance used by first-call benchmarks.</summary>
    private IPerformanceService _initialisedService = null!;

    /// <summary>Creates the service instance used by first-call benchmarks.</summary>
    [IterationSetup(Targets = [nameof(FirstCallConstantRouteAsync), nameof(FirstCallComplexRequestAsync)])]
    public void Setup() => _initialisedService = RestService.For<IPerformanceService>(Host, _settings);

    /// <summary>Benchmarks creating a service instance.</summary>
    /// <returns>The created service.</returns>
    [Benchmark]
    public IPerformanceService CreateService() => RestService.For<IPerformanceService>(Host, _settings);

    /// <summary>Benchmarks the first constant-route call on a pre-created service.</summary>
    /// <returns>The HTTP response.</returns>
    [Benchmark]
    public Task<HttpResponseMessage> FirstCallConstantRouteAsync() => _initialisedService.ConstantRouteAsync();

    /// <summary>Benchmarks creating a service and making a constant-route call.</summary>
    /// <returns>The HTTP response.</returns>
    [Benchmark]
    public Task<HttpResponseMessage> ConstantRouteAsync()
    {
        var service = RestService.For<IPerformanceService>(Host, _settings);
        return service.ConstantRouteAsync();
    }

    /// <summary>Benchmarks the first complex request on a pre-created service.</summary>
    /// <returns>The HTTP response.</returns>
    [Benchmark]
    public Task<HttpResponseMessage> FirstCallComplexRequestAsync() =>
        _initialisedService.ObjectRequestAsync(new() { SomeProperty = "myProperty", SomeQuery = "myQuery" });

    /// <summary>Benchmarks creating a service and making a complex request.</summary>
    /// <returns>The HTTP response.</returns>
    [Benchmark]
    public Task<HttpResponseMessage> ComplexRequestAsync()
    {
        var service = RestService.For<IPerformanceService>(Host, _settings);
        return service.ObjectRequestAsync(new() { SomeProperty = "myProperty", SomeQuery = "myQuery" });
    }
}
