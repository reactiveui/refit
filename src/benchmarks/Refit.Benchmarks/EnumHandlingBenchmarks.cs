// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation micro-benchmarks for enum handling: the <c>EnumHelpers</c> <c>[EnumMember]</c> value lookup and cache
/// build, and the undefined-value numeric formatting.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class EnumHandlingBenchmarks
{
    /// <summary>The runtime enum type inspected by the lookups.</summary>
    private readonly Type _enumType = typeof(QuerySort);

    /// <summary>A boxed enum value carrying an <c>[EnumMember]</c> override.</summary>
    private readonly object _valueWithMember = QuerySort.DateDescending;

    /// <summary>A boxed enum value with no <c>[EnumMember]</c> override.</summary>
    private readonly object _valueWithoutMember = QuerySort.Name;

    /// <summary>An undefined enum value formatted through the numeric path.</summary>
    private readonly QuerySort _undefinedValue = (QuerySort)999;

    /// <summary>Looks up the cached <c>[EnumMember]</c> value for a member that declares one.</summary>
    /// <returns>The looked-up value length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark]
    [BenchmarkCategory("Lookup")]
    public int GetEnumMemberValueWithMember() =>
        (EnumHelpers.GetEnumMemberValue(_enumType, _valueWithMember) ?? string.Empty).Length;

    /// <summary>Looks up the cached <c>[EnumMember]</c> value for a member that declares none.</summary>
    /// <returns>The looked-up value length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark]
    [BenchmarkCategory("Lookup")]
    public int GetEnumMemberValueNoMember() =>
        (EnumHelpers.GetEnumMemberValue(_enumType, _valueWithoutMember) ?? string.Empty).Length;

    /// <summary>Builds the uncached <c>[EnumMember]</c> value map for the enum type.</summary>
    /// <returns>The map entry count.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark]
    [BenchmarkCategory("Build")]
    public int CreateEnumMemberValueMap() => EnumHelpers.CreateEnumMemberValueMap(_enumType).Count;

    /// <summary>Formats an undefined enum value through the numeric backing-type path.</summary>
    /// <returns>The formatted value length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark]
    [BenchmarkCategory("Format")]
    public int FormatNumericValue() => EnumHelpers.Info<QuerySort>.FormatNumericValue(_undefinedValue).Length;
}
