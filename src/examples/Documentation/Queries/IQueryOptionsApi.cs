// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Builds requests to compare delimiter, format and collection choices.</summary>
internal interface IQueryOptionsApi
{
    /// <summary>Flattens dates using a custom prefix and delimiter.</summary>
    /// <param name="dates">The date properties to flatten.</param>
    /// <returns>The unsent request owned by the caller.</returns>
    [Get("/reports")]
    Task<HttpRequestMessage> DatesAsync([Query("-", "filter", "yyyy-MM")] DateFilter dates);

    /// <summary>Formats a scalar amount with the three-argument Query constructor.</summary>
    /// <param name="amount">The amount rendered with two decimal places.</param>
    /// <returns>The unsent request owned by the caller.</returns>
    [Get("/reports")]
    Task<HttpRequestMessage> AmountAsync([Query("-", "filter", "0.00")] decimal amount);

    /// <summary>Uses the settings collection format for scalar elements.</summary>
    /// <param name="tags">The collection to render.</param>
    /// <returns>The unsent request owned by the caller.</returns>
    [Get("/reports")]
    Task<HttpRequestMessage> TagsAsync(string?[] tags);

    /// <summary>Stringifies two objects instead of flattening their properties.</summary>
    /// <param name="phrase">The object explicitly treated as text.</param>
    /// <param name="other">The object whose explicitly empty format also selects ToString.</param>
    /// <returns>The unsent request owned by the caller.</returns>
    [Get("/reports")]
    Task<HttpRequestMessage> TextAsync([Query(TreatAsString = true)] SearchText phrase, [Query(Format = "")] SearchText other);
}
