// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface used to verify handling of <c>204 No Content</c> responses.</summary>
public interface INoContentApi
{
    /// <summary>Gets a list of values.</summary>
    /// <returns>The values, or <see langword="null"/> when no content is returned.</returns>
    [Get("/values")]
    Task<List<string>> GetValues();

    /// <summary>Gets a list of values with response metadata.</summary>
    /// <returns>The API response wrapping the values.</returns>
    [Get("/values")]
    Task<ApiResponse<List<string>>> GetValuesResponse();
}
