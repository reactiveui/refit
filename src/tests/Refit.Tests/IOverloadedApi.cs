// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>API surface with overloaded methods used to verify overload diagnostics.</summary>
public interface IOverloadedApi
{
    /// <summary>Gets the default overloaded endpoint.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/overloaded")]
    Task Overloaded();

    /// <summary>Gets the overloaded endpoint for an id.</summary>
    /// <param name="id">The id path value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/overloaded/{id}")]
    Task Overloaded(int id);
}
