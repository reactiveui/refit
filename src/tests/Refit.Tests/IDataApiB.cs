// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit API that inherits the two-parameter CRUD interface for <see cref="DataEntity"/>.</summary>
public interface IDataApiB : IDataCrudApi<DataEntity, int>
{
    /// <summary>Pings the service.</summary>
    /// <returns>A task that completes when the ping finishes.</returns>
    [Get("")]
    Task PingB();
}
