// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Documentation;

/// <summary>Builds report requests that expose formatter precedence.</summary>
internal interface IFormatterApi
{
    /// <summary>Formats a standalone date and flattened dates using their configured precedence.</summary>
    /// <param name="day">The standalone date using the registered default DateTime format.</param>
    /// <param name="dates">The date properties whose containing-type and explicit formats take precedence.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Get("/reports")]
    Task<HttpRequestMessage> DatesAsync(DateTime day, DateFilter dates);

    /// <summary>Formats a boolean through its type-specific formatter when registered.</summary>
    /// <param name="active">The boolean value formatted through the configured type map.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Get("/reports")]
    Task<HttpRequestMessage> ActiveAsync(bool active);
}
