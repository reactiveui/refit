// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Provides form Url-encoded parameter formatting.</summary>
public interface IFormUrlEncodedParameterFormatter
{
    /// <summary>Formats the specified value.</summary>
    /// <param name="value">The value.</param>
    /// <param name="formatString">The format string.</param>
    /// <returns>The formatted value, or null when <paramref name="value"/> is null.</returns>
    string? Format(object? value, string? formatString);
}
