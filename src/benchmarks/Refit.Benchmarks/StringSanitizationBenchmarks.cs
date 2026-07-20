// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation micro-benchmarks for the shared <c>StringHelpers</c> string routines used on the request path: URI data
/// escaping (both the whole-string and in-place span variants) and the CR/LF header sanitization that runs on every
/// generated header name and value.
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
public class StringSanitizationBenchmarks
{
    /// <summary>The slice start index escaped by the slice benchmark.</summary>
    private const int SliceStart = 2;

    /// <summary>The slice length escaped by the slice benchmark.</summary>
    private const int SliceLength = 20;

    /// <summary>A value with reserved characters that must be percent-encoded.</summary>
    private readonly string _reservedValue = "a value/with spaces & symbols?";

    /// <summary>A header value with no CR or LF, so sanitization returns it unchanged.</summary>
    private readonly string _cleanHeaderValue = "application/json; charset=utf-8";

    /// <summary>A header value carrying embedded CR/LF that sanitization must strip.</summary>
    private readonly string _injectedHeaderValue = "application/json\r\nX-Injected: evil";

    /// <summary>Escapes a whole string for a URI data component.</summary>
    /// <returns>The escaped value length.</returns>
    [Benchmark]
    public int EscapeDataString() => StringHelpers.EscapeDataString(_reservedValue).Length;

    /// <summary>Escapes a slice of a string for a URI data component.</summary>
    /// <returns>The escaped value length.</returns>
    [Benchmark]
    public int EscapeDataStringSlice() => StringHelpers.EscapeDataString(_reservedValue, SliceStart, SliceLength).Length;

    /// <summary>Escapes a value into a stack-backed builder in place, with no intermediate escaped string.</summary>
    /// <returns>The escaped value length.</returns>
    [Benchmark]
    public int AppendUriDataEscaped()
    {
        var builder = new ValueStringBuilder(stackalloc char[256]);
        StringHelpers.AppendUriDataEscaped(ref builder, _reservedValue.AsSpan());
        return builder.ToString().Length;
    }

    /// <summary>Sanitizes a header value that contains no CR or LF (the returns-unchanged fast path).</summary>
    /// <returns>The sanitized value length.</returns>
    [Benchmark(Baseline = true)]
    public int RemoveCrOrLfClean() => StringHelpers.RemoveCrOrLf(_cleanHeaderValue).Length;

    /// <summary>Sanitizes a header value that contains CR/LF (the builder-copy path).</summary>
    /// <returns>The sanitized value length.</returns>
    [Benchmark]
    public int RemoveCrOrLfInjected() => StringHelpers.RemoveCrOrLf(_injectedHeaderValue).Length;
}
