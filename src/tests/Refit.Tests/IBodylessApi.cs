// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface whose methods carry content headers but send no body.</summary>
public interface IBodylessApi
{
    /// <summary>Posts with content headers but no body.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Post("/nobody")]
    [Headers("Content-Type: application/x-www-form-urlencoded; charset=UTF-8")]
    Task Post();

    /// <summary>Gets with content headers but no body.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/nobody")]
    [Headers("Content-Type: application/x-www-form-urlencoded; charset=UTF-8")]
    Task Get();

    /// <summary>Sends a HEAD request with content headers but no body.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Head("/nobody")]
    [Headers("Content-Type: application/x-www-form-urlencoded; charset=UTF-8")]
    Task Head();
}
