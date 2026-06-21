// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit.Tests;

/// <summary>Converts enums to ints and adds a suffix to strings to test key and value formatting.</summary>
public class TestEnumUrlParameterFormatter : DefaultUrlParameterFormatter
{
    /// <summary>Gets the suffix appended to formatted string parameters.</summary>
    public static string StringParameterSuffix => "suffix";

    /// <summary>Formats enums as their backing integer and strings with a suffix.</summary>
    /// <param name="value">The parameter value to format.</param>
    /// <param name="attributeProvider">The attribute provider for the parameter.</param>
    /// <param name="type">The declared type of the parameter.</param>
    /// <returns>The formatted parameter value.</returns>
    public override string? Format(
        object? value,
        ICustomAttributeProvider attributeProvider,
        Type type)
    {
        if (value is TestEnum enumValue)
        {
            var enumBackingValue = (int)enumValue;
            return enumBackingValue.ToString();
        }

        if (value is string stringValue)
        {
            return stringValue + StringParameterSuffix;
        }

        return base.Format(value, attributeProvider, type);
    }
}
