// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit service returning a nullable <see cref="Task{TResult}"/> of a non-nullable reference type.</summary>
public interface IGenericNullableReferenceService
{
    /// <summary>Gets a value whose task itself may be null.</summary>
    /// <returns>A nullable task producing a string.</returns>
    [Get("/")]
    Task<string>? Get();
}
