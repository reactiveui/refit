// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit.Tests.SeparateNamespaceWithModel;

namespace Refit.Tests;

/// <summary>A Refit interface whose signature references a type requiring an extra using directive.</summary>
public interface IAmInterfaceF_RequireUsing
{
    /// <summary>Issues a GET request that returns a model requiring an additional using directive.</summary>
    /// <param name="guids">The identifiers to include in the request.</param>
    /// <returns>The deserialized response model.</returns>
    [Get("/get-requiring-using")]
    Task<ResponseModel> Get(List<Guid> guids);
}
