// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface that sends a decimal query parameter.</summary>
public interface IApiWithDecimal
{
    /// <summary>Gets a response sending a decimal query value.</summary>
    /// <param name="value">The decimal value to send.</param>
    /// <returns>The response body as a string.</returns>
    [Get("/withDecimal")]
    Task<string> GetWithDecimal(decimal value);

    /// <summary>Gets a response sending a decimal query value.</summary>
    /// <param name="value">The decimal value to send.</param>
    /// <returns>The response body as a string.</returns>
    [Get("/withDecimal?value={value}")]
    Task<string> GetWithDecimalGenerated(decimal value);
}
