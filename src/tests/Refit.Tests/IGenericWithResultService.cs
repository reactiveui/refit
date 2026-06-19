// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit service returning a nullable reference type wrapped in a non-nullable <see cref="Task{TResult}"/>.</summary>
public interface IGenericWithResultService
{
    /// <summary>Gets a value whose result is a nullable reference type.</summary>
    /// <returns>A task producing a nullable string.</returns>
    [Get("/")]
    Task<string?> Get();
}
