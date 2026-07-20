// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Reflection.Benchmarks;

/// <summary>
/// EventPipe GC-verbose allocation profile of the per-call request assembly: building the full
/// <see cref="HttpRequestMessage"/> (path, query, headers, body, options) and expanding the relative path across
/// route shapes (constant, dynamic segment, multiple segments, and object-property binding).
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class ReflectionRequestBuildingBenchmarks
{
    /// <summary>The base path used when assembling the relative request URI.</summary>
    private const string BasePath = "/";

    /// <summary>The empty argument list for the constant-route method.</summary>
    private static readonly object[] _constantArgs = [];

    /// <summary>The request builder under test.</summary>
    private RequestBuilderImplementation _builder = null!;

    /// <summary>The parsed constant-route method.</summary>
    private RestMethodInfoInternal _constant = null!;

    /// <summary>The parsed dynamic-route method.</summary>
    private RestMethodInfoInternal _dynamicRoute = null!;

    /// <summary>The parsed multi-segment method.</summary>
    private RestMethodInfoInternal _multiSegment = null!;

    /// <summary>The parsed object-property path method.</summary>
    private RestMethodInfoInternal _objectPath = null!;

    /// <summary>The parsed scalar-query method.</summary>
    private RestMethodInfoInternal _scalarQuery = null!;

    /// <summary>The parsed JSON-body method.</summary>
    private RestMethodInfoInternal _body = null!;

    /// <summary>The dynamic-route arguments.</summary>
    private object[] _dynamicRouteArgs = null!;

    /// <summary>The multi-segment arguments.</summary>
    private object[] _multiSegmentArgs = null!;

    /// <summary>The object-property path arguments.</summary>
    private object[] _objectPathArgs = null!;

    /// <summary>The scalar-query arguments.</summary>
    private object[] _scalarQueryArgs = null!;

    /// <summary>The JSON-body arguments.</summary>
    private object[] _bodyArgs = null!;

    /// <summary>Prepares the builder, parsed methods and argument lists before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var settings = ReflectionBenchmarkFixtures.CreateSettings();
        _builder = new(typeof(IReflectionRequestService), settings);

        _constant = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.ConstantRouteAsync), settings);
        _dynamicRoute = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.UserByIdAsync), settings);
        _multiSegment = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.MultiSegmentAsync), settings);
        _objectPath = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.ObjectPathAsync), settings);
        _scalarQuery = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.ScalarQueryAsync), settings);
        _body = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.CreateUserAsync), settings);

        _dynamicRouteArgs = [ReflectionBenchmarkFixtures.SampleUserId];
        _multiSegmentArgs = [ReflectionBenchmarkFixtures.SampleUserId, "admins", "active"];
        _objectPathArgs =
        [
            new ReflectionQueryModel
            {
                Id = ReflectionBenchmarkFixtures.SampleModelId,
                Name = "widgets",
                Page = ReflectionBenchmarkFixtures.SamplePage,
                Inner = new() { Code = "abc", Label = "l" },
            },
        ];
        _scalarQueryArgs = ["widgets", ReflectionBenchmarkFixtures.SamplePage, true];
        _bodyArgs =
        [
            new ReflectionUserModel
            {
                Id = ReflectionBenchmarkFixtures.SampleBodyUserId,
                Name = "octocat",
                Email = "octo@example.test",
            },
        ];
    }

    /// <summary>Builds the full request message for a constant route.</summary>
    /// <returns>The constructed request message.</returns>
    [Benchmark]
    public Task<HttpRequestMessage> BuildRequestMessageConstantAsync() =>
        _builder.BuildRequestMessageForMethodAsync(_constant, BasePath, false, _constantArgs);

    /// <summary>Builds the full request message for a dynamic route.</summary>
    /// <returns>The constructed request message.</returns>
    [Benchmark]
    public Task<HttpRequestMessage> BuildRequestMessageDynamicRouteAsync() =>
        _builder.BuildRequestMessageForMethodAsync(_dynamicRoute, BasePath, false, _dynamicRouteArgs);

    /// <summary>Builds the full request message for a multi-segment route.</summary>
    /// <returns>The constructed request message.</returns>
    [Benchmark]
    public Task<HttpRequestMessage> BuildRequestMessageMultiSegmentAsync() =>
        _builder.BuildRequestMessageForMethodAsync(_multiSegment, BasePath, false, _multiSegmentArgs);

    /// <summary>Builds the full request message for an object-property route.</summary>
    /// <returns>The constructed request message.</returns>
    [Benchmark]
    public Task<HttpRequestMessage> BuildRequestMessageObjectPathAsync() =>
        _builder.BuildRequestMessageForMethodAsync(_objectPath, BasePath, false, _objectPathArgs);

    /// <summary>Builds the full request message for a scalar-query route.</summary>
    /// <returns>The constructed request message.</returns>
    [Benchmark]
    public Task<HttpRequestMessage> BuildRequestMessageScalarQueryAsync() =>
        _builder.BuildRequestMessageForMethodAsync(_scalarQuery, BasePath, false, _scalarQueryArgs);

    /// <summary>Builds the full request message for a JSON-body route.</summary>
    /// <returns>The constructed request message.</returns>
    [Benchmark]
    public Task<HttpRequestMessage> BuildRequestMessageBodyAsync() =>
        _builder.BuildRequestMessageForMethodAsync(_body, BasePath, false, _bodyArgs);

    /// <summary>Expands the relative path for a constant route.</summary>
    /// <returns>The expanded relative path.</returns>
    [Benchmark]
    public string BuildRelativePathConstant() =>
        _builder.BuildRelativePath(BasePath, _constant, _constantArgs);

    /// <summary>Expands the relative path for a dynamic route.</summary>
    /// <returns>The expanded relative path.</returns>
    [Benchmark]
    public string BuildRelativePathDynamicRoute() =>
        _builder.BuildRelativePath(BasePath, _dynamicRoute, _dynamicRouteArgs);

    /// <summary>Expands the relative path for a multi-segment route.</summary>
    /// <returns>The expanded relative path.</returns>
    [Benchmark]
    public string BuildRelativePathMultiSegment() =>
        _builder.BuildRelativePath(BasePath, _multiSegment, _multiSegmentArgs);

    /// <summary>Expands the relative path for an object-property route.</summary>
    /// <returns>The expanded relative path.</returns>
    [Benchmark]
    public string BuildRelativePathObjectPath() =>
        _builder.BuildRelativePath(BasePath, _objectPath, _objectPathArgs);
}
