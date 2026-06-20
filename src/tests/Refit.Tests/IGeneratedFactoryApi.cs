// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface used to verify registration of a generated factory.</summary>
public interface IGeneratedFactoryApi
{
    /// <summary>Gets the generated endpoint.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/generated")]
    Task Get();

    /// <summary>Gets the generated endpoint with a route parameter.</summary>
    /// <param name="id">The generated endpoint identifier.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/generated/{id}")]
    Task GetById(string id);
}
