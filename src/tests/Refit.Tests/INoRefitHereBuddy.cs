// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>An interface with no Refit attributes, used to verify Refit reports a meaningful error.</summary>
public interface INoRefitHereBuddy
{
    /// <summary>A method with no Refit HTTP attribute.</summary>
    /// <returns>A task that completes when the (non-Refit) call finishes.</returns>
    Task Post();
}
