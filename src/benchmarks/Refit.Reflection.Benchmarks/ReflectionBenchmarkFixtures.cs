// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit.Reflection.Benchmarks;

/// <summary>
/// Shared fixtures for the reflection request-builder micro-benchmarks: default settings and helpers that resolve
/// method metadata on <see cref="IReflectionRequestService"/> and build parsed <see cref="RestMethodInfoInternal"/>
/// instances for the request-building benchmarks.
/// </summary>
internal static class ReflectionBenchmarkFixtures
{
    /// <summary>The base host address used by the reflection request-builder benchmarks.</summary>
    internal const string Host = "https://api.example.test";

    /// <summary>A sample user identifier used as a path/query argument.</summary>
    internal const int SampleUserId = 42;

    /// <summary>A sample model identifier used for object path/query binding.</summary>
    internal const int SampleModelId = 101;

    /// <summary>A sample page number used as a scalar query argument.</summary>
    internal const int SamplePage = 3;

    /// <summary>A sample identifier used for the JSON body model.</summary>
    internal const int SampleBodyUserId = 7;

    /// <summary>A sample trace identifier stored as a request property.</summary>
    internal const string SampleTraceId = "trace-123";

    /// <summary>Creates the default settings used across the reflection benchmarks.</summary>
    /// <returns>The Refit settings.</returns>
    internal static RefitSettings CreateSettings() => new(new SystemTextJsonContentSerializer());

    /// <summary>Resolves a declared method on the sample interface by name.</summary>
    /// <param name="name">The method name.</param>
    /// <returns>The reflected method information.</returns>
    internal static MethodInfo Method(string name) =>
        typeof(IReflectionRequestService).GetMethod(name)
        ?? throw new MissingMethodException(nameof(IReflectionRequestService), name);

    /// <summary>Gets the reflected parameters of a sample interface method, excluding cancellation tokens.</summary>
    /// <param name="name">The method name.</param>
    /// <returns>The parameters used for request mapping.</returns>
    internal static ParameterInfo[] MappedParameters(string name) =>
        RestMethodInfoInternal.GetNonCancellationTokenParameters(Method(name).GetParameters());

    /// <summary>Builds the parsed metadata for a sample interface method.</summary>
    /// <param name="name">The method name.</param>
    /// <param name="settings">The settings to use.</param>
    /// <returns>The parsed method metadata.</returns>
    internal static RestMethodInfoInternal Build(string name, RefitSettings settings) =>
        new(typeof(IReflectionRequestService), Method(name), settings);
}
