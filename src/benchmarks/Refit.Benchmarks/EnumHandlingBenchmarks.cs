// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation micro-benchmarks for enum handling: the <c>EnumHelpers</c> <c>[EnumMember]</c> value lookup and cache
/// build, the undefined-value numeric formatting, the <c>CamelCaseStringEnumConverter</c> name helpers and
/// serialized-name dictionary construction, and a JSON round-trip through the camelCase enum converter.
/// </summary>
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

    /// <summary>A defined enum value serialized through the converter.</summary>
    private readonly QuerySort _definedValue = QuerySort.Name;

    /// <summary>A Pascal-cased name converted to camelCase.</summary>
    private readonly string _pascalName = "DateDescending";

    /// <summary>The enum field carrying an <c>[EnumMember]</c> override.</summary>
    private FieldInfo _memberField = null!;

    /// <summary>Options carrying the camelCase enum converter used by the round-trip benchmarks.</summary>
    private JsonSerializerOptions _options = null!;

    /// <summary>The serialized camelCase name deserialized by the round-trip benchmark.</summary>
    private string _serializedName = null!;

    /// <summary>Resolves the reflected field and serializer options before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _memberField = _enumType.GetField(nameof(QuerySort.DateDescending))!;
        _options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
        _serializedName = JsonSerializer.Serialize(_definedValue, _options);
    }

    /// <summary>Looks up the cached <c>[EnumMember]</c> value for a member that declares one.</summary>
    /// <returns>The looked-up value length.</returns>
    [Benchmark]
    [BenchmarkCategory("Lookup")]
    public int GetEnumMemberValueWithMember() =>
        (EnumHelpers.GetEnumMemberValue(_enumType, _valueWithMember) ?? string.Empty).Length;

    /// <summary>Looks up the cached <c>[EnumMember]</c> value for a member that declares none.</summary>
    /// <returns>The looked-up value length.</returns>
    [Benchmark]
    [BenchmarkCategory("Lookup")]
    public int GetEnumMemberValueNoMember() =>
        (EnumHelpers.GetEnumMemberValue(_enumType, _valueWithoutMember) ?? string.Empty).Length;

    /// <summary>Builds the uncached <c>[EnumMember]</c> value map for the enum type.</summary>
    /// <returns>The map entry count.</returns>
    [Benchmark]
    [BenchmarkCategory("Build")]
    public int CreateEnumMemberValueMap() => EnumHelpers.CreateEnumMemberValueMap(_enumType).Count;

    /// <summary>Constructs the camelCase enum converter, building all three serialized-name maps from a single field scan.</summary>
    /// <returns>The names-to-values entry count, forcing the maps to be built.</returns>
    [Benchmark]
    [BenchmarkCategory("Build")]
    public int ConstructEnumConverter()
    {
        // Reading a shared field anchors this construction benchmark to the instance so BenchmarkDotNet can invoke it.
        _ = _enumType;
        return new CamelCaseStringEnumConverter.EnumConverter<QuerySort>().MapEntryCount;
    }

    /// <summary>Formats an undefined enum value through the numeric backing-type path.</summary>
    /// <returns>The formatted value length.</returns>
    [Benchmark]
    [BenchmarkCategory("Format")]
    public int FormatNumericValue() => EnumHelpers.Info<QuerySort>.FormatNumericValue(_undefinedValue).Length;

    /// <summary>Converts a Pascal-cased name to camelCase.</summary>
    /// <returns>The converted value length.</returns>
    [Benchmark]
    [BenchmarkCategory("Format")]
    public int ToCamelCase() => CamelCaseStringEnumConverter.ToCamelCase(_pascalName).Length;

    /// <summary>Resolves the preferred serialized name for an enum field.</summary>
    /// <returns>The resolved name length.</returns>
    [Benchmark]
    [BenchmarkCategory("Format")]
    public int GetPreferredSerializedName() => CamelCaseStringEnumConverter.GetPreferredSerializedName(_memberField).Length;

    /// <summary>Serializes a defined enum value through the camelCase converter.</summary>
    /// <returns>The serialized name length.</returns>
    [Benchmark]
    [BenchmarkCategory("RoundTrip")]
    public int SerializeEnum() => JsonSerializer.Serialize(_definedValue, _options).Length;

    /// <summary>Deserializes a camelCase name back to its enum value through the converter.</summary>
    /// <returns>The deserialized enum value as an integer.</returns>
    [Benchmark]
    [BenchmarkCategory("RoundTrip")]
    public int DeserializeEnum() => (int)JsonSerializer.Deserialize<QuerySort>(_serializedName, _options);
}
