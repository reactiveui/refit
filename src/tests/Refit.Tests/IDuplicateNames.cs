// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Refit fixture interface with two overloads of the same method name to exercise cache keying.</summary>
public interface IDuplicateNames
{
    /// <summary>Sends a POST request with a single string parameter.</summary>
    /// <param name="id">The identifier value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo")]
    Task SingleParameter(string id);

    /// <summary>Sends a POST request with a single integer parameter.</summary>
    /// <param name="id">The identifier value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo")]
    Task SingleParameter(int id);
}
