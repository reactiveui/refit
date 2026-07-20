// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Refit.Tests;

/// <summary>Tests for <see cref="DefaultFormUrlEncodedParameterFormatter"/>.</summary>
public class DefaultFormUrlEncodedParameterFormatterTests
{
    /// <summary>An enum value that is not defined on <see cref="FormEnum"/>.</summary>
    private const int UndefinedEnumValue = 999;

    /// <summary>A representative string value used to verify verbatim string formatting.</summary>
    private const string StringValue = "widgets and gadgets";

    /// <summary>A representative integer value used to verify invariant integer formatting.</summary>
    private const int IntegerValue = 1234;

    /// <summary>Enum used to verify enum-member formatting and undefined enum fallback.</summary>
    private enum FormEnum
    {
        /// <summary>The value with an explicit form name.</summary>
        [EnumMember(Value = "custom-value")]
        Custom = 1,

        /// <summary>A value without an explicit form name.</summary>
        Plain = 2
    }

    /// <summary>Verifies null values format without allocating a string.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FormatReturnsNullForNullValue()
    {
        var formatter = new DefaultFormUrlEncodedParameterFormatter();

        await Assert.That(formatter.Format(null, null)).IsNull();
    }

    /// <summary>Verifies enum-member names are honored and undefined values fall back to their numeric value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FormatUsesEnumMemberWhenPresentAndFallsBackForUndefinedValues()
    {
        var formatter = new DefaultFormUrlEncodedParameterFormatter();

        await Assert.That(formatter.Format(FormEnum.Custom, null)).IsEqualTo("custom-value");
        await Assert.That(formatter.Format(FormEnum.Plain, null)).IsEqualTo("Plain");
        await Assert.That(formatter.Format((FormEnum)UndefinedEnumValue, null)).IsEqualTo("999");
    }

    /// <summary>Verifies a string value is returned verbatim, ignoring any format string, matching composite formatting.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FormatReturnsStringValueVerbatimIgnoringFormat()
    {
        var formatter = new DefaultFormUrlEncodedParameterFormatter();

        await Assert.That(formatter.Format(StringValue, null)).IsEqualTo(StringValue);
        await Assert.That(formatter.Format(StringValue, "X")).IsEqualTo(StringValue);
    }

    /// <summary>Verifies formattable values render with the invariant culture, apply a format string, and treat a
    /// whitespace-only format string as no format.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FormatRendersFormattableValuesInvariantlyAndAppliesFormat()
    {
        const int sample = 7;
        const double fraction = 1.5;
        var formatter = new DefaultFormUrlEncodedParameterFormatter();

        await Assert.That(formatter.Format(IntegerValue, null)).IsEqualTo("1234");
        await Assert.That(formatter.Format(sample, "D3")).IsEqualTo("007");
        await Assert.That(formatter.Format(sample, "  ")).IsEqualTo("7");
        await Assert.That(formatter.Format(fraction, null)).IsEqualTo("1.5");
        await Assert.That(formatter.Format(true, null)).IsEqualTo("True");
        await Assert.That(formatter.Format('a', null)).IsEqualTo("a");
    }
}
