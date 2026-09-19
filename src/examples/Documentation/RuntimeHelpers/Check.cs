// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.RuntimeHelpers;

/// <summary>Turns an unexpected demonstration result into a failure.</summary>
internal static class Check
{
    /// <summary>Requires the documented contract to hold.</summary>
    /// <param name="result">Whether the contract was observed.</param>
    /// <param name="message">The failed contract.</param>
    /// <exception cref="InvalidOperationException">The observed result differs from the documented contract.</exception>
    internal static void Require(bool result, string message)
    {
        if (!result)
        {
            throw new InvalidOperationException(message);
        }
    }
}
