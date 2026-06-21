// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace Refit.Tests;

/// <summary>Tests for enum formatting helpers used by serializers and form formatters.</summary>
public sealed class EnumHelpersTests
{
    /// <summary>Enum with an attributed member.</summary>
    private enum MemberEnum
    {
        /// <summary>Default value.</summary>
        None = 0,

        /// <summary>Plain member name.</summary>
        Plain = 1,

        /// <summary>Custom serialized member name.</summary>
        [EnumMember(Value = "custom-value")]
        Custom = 2
    }

    /// <summary>Signed byte-backed enum.</summary>
    [SuppressMessage("Major Code Smell", "S4022:Enumerations should have Int32 storage", Justification = "This fixture verifies sbyte-backed enum handling.")]
    private enum SByteEnum : sbyte
    {
        /// <summary>Default value.</summary>
        None = 0,

        /// <summary>Negative value.</summary>
        Value = -8
    }

    /// <summary>Byte-backed enum.</summary>
    [SuppressMessage("Major Code Smell", "S4022:Enumerations should have Int32 storage", Justification = "This fixture verifies byte-backed enum handling.")]
    private enum ByteEnum : byte
    {
        /// <summary>Default value.</summary>
        None = 0,

        /// <summary>High byte value.</summary>
        Value = 250
    }

    /// <summary>Signed 16-bit enum.</summary>
    [SuppressMessage("Major Code Smell", "S4022:Enumerations should have Int32 storage", Justification = "This fixture verifies short-backed enum handling.")]
    private enum Int16Enum : short
    {
        /// <summary>Default value.</summary>
        None = 0,

        /// <summary>Negative value.</summary>
        Value = -1234
    }

    /// <summary>Unsigned 16-bit enum.</summary>
    [SuppressMessage("Major Code Smell", "S4022:Enumerations should have Int32 storage", Justification = "This fixture verifies ushort-backed enum handling.")]
    private enum UInt16Enum : ushort
    {
        /// <summary>Default value.</summary>
        None = 0,

        /// <summary>High unsigned value.</summary>
        Value = 65_000
    }

    /// <summary>Signed 32-bit enum.</summary>
    private enum Int32Enum
    {
        /// <summary>Default value.</summary>
        None = 0,

        /// <summary>Negative value.</summary>
        Value = -123_456
    }

    /// <summary>Unsigned 32-bit enum.</summary>
    [SuppressMessage("Major Code Smell", "S4022:Enumerations should have Int32 storage", Justification = "This fixture verifies uint-backed enum handling.")]
    private enum UInt32Enum : uint
    {
        /// <summary>Default value.</summary>
        None = 0,

        /// <summary>High unsigned value.</summary>
        Value = 4_000_000_000
    }

    /// <summary>Signed 64-bit enum.</summary>
    [SuppressMessage("Major Code Smell", "S4022:Enumerations should have Int32 storage", Justification = "This fixture verifies long-backed enum handling.")]
    private enum Int64Enum : long
    {
        /// <summary>Default value.</summary>
        None = 0,

        /// <summary>Negative value.</summary>
        Value = -1_234_567_890_123
    }

    /// <summary>Unsigned 64-bit enum.</summary>
    [SuppressMessage("Major Code Smell", "S4022:Enumerations should have Int32 storage", Justification = "This fixture verifies ulong-backed enum handling.")]
    private enum UInt64Enum : ulong
    {
        /// <summary>Default value.</summary>
        None = 0,

        /// <summary>Maximum unsigned value.</summary>
        Value = ulong.MaxValue
    }

    /// <summary>Verifies enum-member lookup handles enum, non-enum, and undefined enum values.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetEnumMemberValueHandlesAttributeAndFallbackCases()
    {
        await Assert.That(EnumHelpers.GetEnumMemberValue(typeof(string), "value")).IsNull();
        await Assert.That(EnumHelpers.GetEnumMemberValue(typeof(MemberEnum), (MemberEnum)999)).IsNull();
        await Assert.That(EnumHelpers.GetEnumMemberValue(typeof(MemberEnum), MemberEnum.Plain)).IsNull();
        await Assert.That(EnumHelpers.GetEnumMemberValue(typeof(MemberEnum), MemberEnum.Custom)).IsEqualTo("custom-value");
    }

    /// <summary>Verifies numeric enum helpers preserve every supported signed and unsigned backing type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NumericHelpersSupportAllEnumBackingTypes()
    {
        await AssertNumericEnum(SByteEnum.Value, "-8", "-8");
        await AssertNumericEnum(ByteEnum.Value, "250", "250");
        await AssertNumericEnum(Int16Enum.Value, "-1234", "-1234");
        await AssertNumericEnum(UInt16Enum.Value, "65000", "65000");
        await AssertNumericEnum(Int32Enum.Value, "-123456", "-123456");
        await AssertNumericEnum(UInt32Enum.Value, "4000000000", "4000000000");
        await AssertNumericEnum(Int64Enum.Value, "-1234567890123", "-1234567890123");
        await AssertNumericEnum(UInt64Enum.Value, "18446744073709551615", "18446744073709551615");
    }

    /// <summary>Verifies parsing enum names uses the cached generic enum path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ParseNameReturnsDeclaredEnumValue() =>
        await Assert.That(EnumHelpers.Info<MemberEnum>.ParseName(nameof(MemberEnum.Custom))).IsEqualTo(MemberEnum.Custom);

    /// <summary>Verifies numeric enum read, write, and formatting helpers agree for a value.</summary>
    /// <typeparam name="TEnum">The enum type under test.</typeparam>
    /// <param name="expected">The expected enum value.</param>
    /// <param name="json">The JSON numeric literal.</param>
    /// <param name="formatted">The expected formatted value.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    private static async Task AssertNumericEnum<TEnum>(
        TEnum expected,
        string json,
        string formatted)
        where TEnum : struct, Enum
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();

        var read = EnumHelpers.Info<TEnum>.ReadJsonNumericValue(ref reader);

        await Assert.That(read).IsEqualTo(expected);
        await Assert.That(EnumHelpers.Info<TEnum>.FormatNumericValue(expected)).IsEqualTo(formatted);

        await using var stream = new MemoryStream();
        await using (var writer = new Utf8JsonWriter(stream))
        {
            EnumHelpers.Info<TEnum>.WriteJsonNumericValue(writer, expected);
        }

        await Assert.That(Encoding.UTF8.GetString(stream.ToArray())).IsEqualTo(formatted);
    }
}
