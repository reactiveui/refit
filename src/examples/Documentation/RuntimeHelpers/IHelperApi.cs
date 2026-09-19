// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.RuntimeHelpers;

/// <summary>Provides a normal interface whose generated name can be compared with helper output.</summary>
internal interface IHelperApi
{
    /// <summary>Reads one integer from the demonstration endpoint.</summary>
    /// <param name="id">The route identifier.</param>
    /// <returns>The endpoint's integer payload.</returns>
    [Get("/items/{id}")]
    Task<int> GetAsync(int id);
}
