// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit service accepting both a nullable reference parameter and a nullable value parameter.</summary>
public interface IReferenceAndValueParametersService
{
    /// <summary>Sends a request with nullable reference and value parameters.</summary>
    /// <param name="reference">A nullable reference-type argument.</param>
    /// <param name="value">A nullable value-type argument.</param>
    /// <returns>A task representing the request.</returns>
    [Get("/")]
    Task Get(string? reference, int? value);
}
