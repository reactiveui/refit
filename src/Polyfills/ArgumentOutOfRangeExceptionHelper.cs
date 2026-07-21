// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Refit.Internal;

/// <summary>Polyfill for modern <see cref="ArgumentOutOfRangeException"/> guard helpers on target frameworks that predate them.</summary>
[ExcludeFromCodeCoverage]
internal static class ArgumentOutOfRangeExceptionHelper
{
    /// <summary>Throws when <paramref name="value"/> is negative.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    internal static void ThrowIfNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value >= 0)
        {
            return;
        }

        throw new ArgumentOutOfRangeException(paramName, value, null);
    }

    /// <summary>Throws when <paramref name="value"/> is negative or zero.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    internal static void ThrowIfNegativeOrZero(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value > 0)
        {
            return;
        }

        throw new ArgumentOutOfRangeException(paramName, value, null);
    }
}
