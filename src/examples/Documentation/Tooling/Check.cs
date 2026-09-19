// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Tooling;

/// <summary>Turns failed demonstration checks into executable failures.</summary>
internal static class Check
{
    /// <summary>Throws when the demonstrated contract is not satisfied.</summary>
    /// <param name="result">Whether the expected behavior was observed.</param>
    /// <param name="message">The failed contract.</param>
    /// <exception cref="InvalidOperationException">The demonstrated result is incorrect.</exception>
    internal static void Require(bool result, string message)
    {
        if (!result)
        {
            throw new InvalidOperationException(message);
        }
    }
}
