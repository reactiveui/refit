// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface returning a <see cref="ValueTask{TResult}"/> of <see cref="ApiResponse{T}"/>.</summary>
public interface IValueTaskApiResponseApi
{
    /// <summary>Gets a value with response metadata for the supplied key.</summary>
    /// <param name="value">The path value to retrieve.</param>
    /// <returns>The API response wrapping the retrieved value.</returns>
    [Get("/{value}")]
    ValueTask<ApiResponse<string>> GetValue(string value);
}
