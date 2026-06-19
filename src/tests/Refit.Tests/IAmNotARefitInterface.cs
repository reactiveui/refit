// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>An interface with no Refit attributes, used to confirm the generator ignores it.</summary>
public interface IAmNotARefitInterface
{
    /// <summary>A plain method carrying no Refit attribute.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotARefitMethod();
}
