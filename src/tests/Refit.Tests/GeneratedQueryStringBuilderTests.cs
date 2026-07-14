// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Numerics;

namespace Refit.Tests;

/// <summary>Verifies the null-omission and collection-delimiter behavior of <see cref="GeneratedQueryStringBuilder"/>.</summary>
public sealed class GeneratedQueryStringBuilderTests
{
    /// <summary>The relative path shared by the builder fixtures.</summary>
    private const string Path = "/x";

    /// <summary>The query key shared by the builder fixtures.</summary>
    private const string Key = "k";

    /// <summary>The number of trailing zeros in the buffer-overflowing value, chosen to exceed both the 128-char stack
    /// buffer and the first 256-char rented buffer so the format loop grows twice and returns the earlier rented buffer.</summary>
    private const int LongValueZeroCount = 300;

    /// <summary>The base of the power used to build the buffer-overflowing value.</summary>
    private const int PowerBase = 10;

    /// <summary>The number of trailing zeros in a value that overflows the 128-char stack buffer but fits the first
    /// 256-char rented buffer, so the format loop grows exactly once.</summary>
    private const int SingleGrowthZeroCount = 200;

    /// <summary>The relative path and key prefix of the rendered buffer-overflowing value.</summary>
    private const string LongValueQueryPrefix = "/x?k=1";

    /// <summary>Verifies a null value omits its parameter, leaving the path unchanged.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AddOmitsParameterForNullValue()
    {
        var result = AddValue(Key, null);

        await Assert.That(result).IsEqualTo(Path);
    }

    /// <summary>Verifies a null flag name omits the flag, leaving the path unchanged.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AddFlagOmitsNullFlagName()
    {
        var result = AddNullFlag();

        await Assert.That(result).IsEqualTo(Path);
    }

    /// <summary>Verifies a non-null flag name is escaped and appended as a valueless flag.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AddFlagEscapesNonNullFlagName()
    {
        var builder = new GeneratedQueryStringBuilder(Path);
        builder.AddFlag("a b", false);

        await Assert.That(builder.Build()).IsEqualTo("/x?a%20b");
    }

    /// <summary>Verifies a pre-encoded flag name is appended verbatim without escaping.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AddFlagAppendsPreEncodedFlagNameVerbatim()
    {
        var builder = new GeneratedQueryStringBuilder(Path);
        builder.AddFlag("a+b", true);

        await Assert.That(builder.Build()).IsEqualTo("/x?a+b");
    }

    /// <summary>Verifies a space-separated collection joins its values with a space.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BeginCollectionJoinsSpaceSeparatedValues()
    {
        var result = JoinCollection(CollectionFormat.Ssv);

        await Assert.That(result).IsEqualTo("/x?k=a%20b");
    }

    /// <summary>Verifies a tab-separated collection joins its values with a tab.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BeginCollectionJoinsTabSeparatedValues()
    {
        var result = JoinCollection(CollectionFormat.Tsv);

        await Assert.That(result).IsEqualTo("/x?k=a%09b");
    }

    /// <summary>Verifies a pipe-separated collection joins its values with a pipe.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BeginCollectionJoinsPipeSeparatedValues()
    {
        var result = JoinCollection(CollectionFormat.Pipes);

        await Assert.That(result).IsEqualTo("/x?k=a%7Cb");
    }

    /// <summary>Verifies a comma-separated (default) collection joins its values with a comma.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BeginCollectionJoinsCommaSeparatedValues()
    {
        var result = JoinCollection(CollectionFormat.Csv);

        await Assert.That(result).IsEqualTo("/x?k=a%2Cb");
    }

    /// <summary>Verifies a span-formattable value rendered with an explicit compile-time format is padded per the format.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AddFormattedAppliesCompileTimeFormat()
    {
        const int value = 42;
        var builder = new GeneratedQueryStringBuilder(Path);
        builder.AddFormatted(Key, value, "D5", false);

        await Assert.That(builder.Build()).IsEqualTo("/x?k=00042");
    }

    /// <summary>Verifies a value rendered with an explicit compile-time format that overflows both the stack buffer and
    /// the first rented buffer grows the rented buffer and still renders in full.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AddFormattedGrowsRentedBufferForLongFormattedValue()
    {
        var value = BigInteger.Pow(PowerBase, LongValueZeroCount);
        var builder = new GeneratedQueryStringBuilder(Path);
        builder.AddFormatted(Key, value, "R", false);

        var expected = LongValueQueryPrefix + new string('0', LongValueZeroCount);
        await Assert.That(builder.Build()).IsEqualTo(expected);
    }

    /// <summary>Verifies a formatted collection element that overflows the stack and first rented buffer grows the
    /// rented buffer and still renders in full, exercising the unescaped join path of the format loop.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AddCollectionValueFormattedGrowsRentedBufferForLongValue()
    {
        var value = BigInteger.Pow(PowerBase, LongValueZeroCount);
        var builder = new GeneratedQueryStringBuilder(Path);
        builder.BeginCollection(Key, CollectionFormat.Csv, false);
        builder.AddCollectionValueFormatted(value);
        builder.EndCollection();

        var expected = LongValueQueryPrefix + new string('0', LongValueZeroCount);
        await Assert.That(builder.Build()).IsEqualTo(expected);
    }

    /// <summary>Verifies a value that overflows the stack buffer but fits the first rented buffer grows the rented
    /// buffer exactly once and still renders in full.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AddFormattedGrowsRentedBufferOnceForMidLengthValue()
    {
        var value = BigInteger.Pow(PowerBase, SingleGrowthZeroCount);
        var builder = new GeneratedQueryStringBuilder(Path);
        builder.AddFormatted(Key, value, null, false);

        var expected = LongValueQueryPrefix + new string('0', SingleGrowthZeroCount);
        await Assert.That(builder.Build()).IsEqualTo(expected);
    }

    /// <summary>Verifies a pre-escaped key with a null value omits the parameter, leaving the path unchanged.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AddPreEscapedKeyOmitsParameterForNullValue()
    {
        var builder = new GeneratedQueryStringBuilder(Path);
        builder.AddPreEscapedKey(Key, null, false);

        await Assert.That(builder.Build()).IsEqualTo(Path);
    }

    /// <summary>Verifies a span-formattable value is rendered straight into the query.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AddFormattedRendersSpanFormattableValue()
    {
        const int value = 42;
        var builder = new GeneratedQueryStringBuilder(Path);
        builder.AddFormatted(Key, value, null, false);

        await Assert.That(builder.Build()).IsEqualTo("/x?k=42");
    }

    /// <summary>Verifies a span-formattable value longer than the stack buffer and first rented buffer grows the rented
    /// buffer twice and still renders in full.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AddFormattedGrowsRentedBufferForLongValue()
    {
        var value = BigInteger.Pow(PowerBase, LongValueZeroCount);
        var builder = new GeneratedQueryStringBuilder(Path);
        builder.AddFormatted(Key, value, null, false);

        var expected = LongValueQueryPrefix + new string('0', LongValueZeroCount);
        await Assert.That(builder.Build()).IsEqualTo(expected);
    }

    /// <summary>Appends a single value and returns the built relative path.</summary>
    /// <param name="name">The query key.</param>
    /// <param name="value">The value, or null to omit the parameter.</param>
    /// <returns>The built relative path.</returns>
    private static string AddValue(string name, string? value)
    {
        var builder = new GeneratedQueryStringBuilder(Path);
        builder.Add(name, value, false);
        return builder.Build();
    }

    /// <summary>Appends a null flag and returns the built relative path.</summary>
    /// <returns>The built relative path.</returns>
    private static string AddNullFlag()
    {
        var builder = new GeneratedQueryStringBuilder(Path);
        builder.AddFlag(null, false);
        return builder.Build();
    }

    /// <summary>Joins a two-element collection under the given format and returns the built relative path.</summary>
    /// <param name="collectionFormat">The resolved collection format.</param>
    /// <returns>The built relative path.</returns>
    private static string JoinCollection(CollectionFormat collectionFormat)
    {
        var builder = new GeneratedQueryStringBuilder(Path);
        builder.BeginCollection(Key, collectionFormat, false);
        builder.AddCollectionValue("a");
        builder.AddCollectionValue("b");
        builder.EndCollection();
        return builder.Build();
    }
}
