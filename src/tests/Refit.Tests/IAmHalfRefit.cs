// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>
/// An interface that mixes a Refit method with a non-Refit method, used to verify Refit
/// reports a meaningful error for the non-Refit member.
/// </summary>
public interface IAmHalfRefit
{
    /// <summary>A valid Refit POST method.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Post("/anything")]
    Task Post();

    /// <summary>A method intentionally missing a Refit HTTP attribute.</summary>
    /// <returns>A task that completes when the (non-Refit) call finishes.</returns>
    [SuppressMessage("Refit", "RF001", Justification = "Intentional non-Refit fixture used to verify generator diagnostics.")]
    Task Get();
}
