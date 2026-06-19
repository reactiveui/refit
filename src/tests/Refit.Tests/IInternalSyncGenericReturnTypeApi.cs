// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>API surface exercising an internal synchronous generic return type.</summary>
public interface IInternalSyncGenericReturnTypeApi
{
    /// <summary>Gets the values.</summary>
    /// <returns>The list of values returned by the endpoint.</returns>
    [Get("/values")]
    internal List<string> GetValues();
}
