// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A generic CRUD Refit API keyed by <see cref="long"/> that adds a copy operation.</summary>
/// <typeparam name="T">The entity type managed by the API.</typeparam>
public interface IDataCrudApi<T> : IDataCrudApi<T, long>
    where T : class
{
    /// <summary>Creates a copy of the supplied entity.</summary>
    /// <param name="payload">The entity to copy.</param>
    /// <returns>The copied entity.</returns>
    [Post("")]
    Task<T> Copy([Body] T payload);
}
