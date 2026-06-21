// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;

namespace Refit.Tests;

/// <summary>Joins enumerable parameter values into a comma-separated formatted string.</summary>
public class TestEnumerableUrlParameterFormatter : DefaultUrlParameterFormatter
{
    /// <summary>Formats enumerable values by joining their formatted elements with commas.</summary>
    /// <param name="value">The parameter value to format.</param>
    /// <param name="attributeProvider">The attribute provider for the parameter.</param>
    /// <param name="type">The declared type of the parameter.</param>
    /// <returns>The formatted parameter value.</returns>
    public override string? Format(
        object? value,
        ICustomAttributeProvider attributeProvider,
        Type type)
    {
        if (value is IEnumerable<object> enu)
        {
            return string.Join(",", enu.Select(o => base.Format(o, attributeProvider, type)));
        }

        if (value is IEnumerable en)
        {
            return string.Join(
                ",",
                en.Cast<object>().Select(o => base.Format(o, attributeProvider, type)));
        }

        return base.Format(value, attributeProvider, type);
    }
}
