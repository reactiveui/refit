// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit service returning a nullable <see cref="Task{TResult}"/> of a nullable reference type.</summary>
public interface IGenericNullableWithNullableReferenceService
{
    /// <summary>Gets a value where both the task and its result may be null.</summary>
    /// <returns>A nullable task producing a nullable string.</returns>
    [Get("/")]
    Task<string?>? Get();
}
