// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit service returning a nullable <see cref="ValueTask{TResult}"/> of a non-nullable value type.</summary>
public interface IGenericNullableValueService
{
    /// <summary>Gets a value whose value task itself may be null.</summary>
    /// <returns>A nullable value task producing an integer.</returns>
    [Get("/")]
    ValueTask<int>? Get();
}
