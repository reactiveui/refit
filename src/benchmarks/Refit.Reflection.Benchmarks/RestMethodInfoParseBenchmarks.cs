// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Reflection.Benchmarks;

/// <summary>
/// EventPipe GC-verbose allocation profile of the one-time-per-method reflective metadata parse
/// (<see cref="RestMethodInfoInternal"/> construction) across representative interface method shapes: dynamic
/// route, scalar query, object query, headers, JSON body, multipart, and nested-object path binding.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class RestMethodInfoParseBenchmarks
{
    /// <summary>The settings used when parsing method metadata.</summary>
    private RefitSettings _settings = null!;

    /// <summary>Initializes the settings before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup() => _settings = ReflectionBenchmarkFixtures.CreateSettings();

    /// <summary>Parses a method with a single dynamic route segment.</summary>
    /// <returns>The parsed method metadata.</returns>
    [Benchmark]
    public object ParseDynamicRoute() =>
        ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.UserByIdAsync), _settings);

    /// <summary>Parses a method with several scalar query parameters.</summary>
    /// <returns>The parsed method metadata.</returns>
    [Benchmark]
    public object ParseScalarQuery() =>
        ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.ScalarQueryAsync), _settings);

    /// <summary>Parses a method whose object argument is flattened into the query string.</summary>
    /// <returns>The parsed method metadata.</returns>
    [Benchmark]
    public object ParseObjectQuery() =>
        ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.ObjectQueryAsync), _settings);

    /// <summary>Parses a method with static, per-parameter, collection and authorization headers.</summary>
    /// <returns>The parsed method metadata.</returns>
    [Benchmark]
    public object ParseHeaderMethod() =>
        ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.HeaderAsync), _settings);

    /// <summary>Parses a method that serializes an object body.</summary>
    /// <returns>The parsed method metadata.</returns>
    [Benchmark]
    public object ParseBodyMethod() =>
        ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.CreateUserAsync), _settings);

    /// <summary>Parses a multipart upload method.</summary>
    /// <returns>The parsed method metadata.</returns>
    [Benchmark]
    public object ParseMultipartMethod() =>
        ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.UploadAsync), _settings);

    /// <summary>Parses a method whose route is bound to a nested object property chain.</summary>
    /// <returns>The parsed method metadata.</returns>
    [Benchmark]
    public object ParseNestedObjectPath() =>
        ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.NestedPathAsync), _settings);
}
