// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>A Refit interface whose path parameter is formatted through its <c>[Query(Format = ...)]</c> attribute.</summary>
public interface IReflectionQueryFormatApi
{
    /// <summary>Fetches a resource whose identifier is formatted into the path.</summary>
    /// <param name="id">The identifier, formatted with one decimal place.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithQueryFormat([Query(Format = "0.0")] int id);
}
