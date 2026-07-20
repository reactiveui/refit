// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Default form Url-encoded parameter formatter.</summary>
public class DefaultFormUrlEncodedParameterFormatter : IFormUrlEncodedParameterFormatter
{
    /// <summary>Formats the specified parameter value.</summary>
    /// <param name="value">The parameter value.</param>
    /// <param name="formatString">The format string.</param>
    /// <returns>The formatted value, or null when <paramref name="value"/> is null.</returns>
    public virtual string? Format(object? value, string? formatString)
    {
        if (value is null)
        {
            return null;
        }

        var parameterType = value.GetType();
        var enumMemberValue = EnumHelpers.GetEnumMemberValue(parameterType, value);

        return InvariantValueRenderer.Render(enumMemberValue ?? value, formatString);
    }
}
