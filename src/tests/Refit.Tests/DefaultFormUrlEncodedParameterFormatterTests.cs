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
}
