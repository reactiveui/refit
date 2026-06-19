// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>Intentional fixture mixing a Refit method with a non-Refit attribute to test the generator.</summary>
public interface IFooWithOtherAttribute
{
    /// <summary>Gets the root resource.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/")]
    Task GetRoot();

    /// <summary>A method deliberately decorated with a non-Refit attribute.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [SuppressMessage("Refit", "RF001", Justification = "Intentional non-Refit fixture used to verify generator diagnostics.")]
    [System.ComponentModel.DisplayName("/")]
    Task PostRoot();
}
