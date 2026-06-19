// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>API surface exercising an internal synchronous IApiResponse return type.</summary>
public interface IInternalSyncGenericApiResponseReturnTypeApi
{
    /// <summary>Gets the response.</summary>
    /// <returns>The API response wrapping the string payload.</returns>
    [Get("/response")]
    internal IApiResponse<string> GetResponse();
}
