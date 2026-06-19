// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit service whose parameter is a nullable generic collection of non-nullable elements.</summary>
public interface IGenericNullableReferenceParameterService
{
    /// <summary>Sends a request with a nullable list parameter.</summary>
    /// <param name="reference">A nullable list of strings.</param>
    /// <returns>A task representing the request.</returns>
    [Get("/")]
    Task Get(List<string>? reference);
}
