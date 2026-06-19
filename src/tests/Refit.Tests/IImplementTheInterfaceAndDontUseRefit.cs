// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A fixture interface that re-declares an inherited Refit member without its own HTTP attribute.</summary>
public interface IImplementTheInterfaceAndDontUseRefit : IAmInterfaceD
{
    /// <summary>Hides the inherited <see cref="IAmInterfaceD.Test"/> without supplying a Refit attribute.</summary>
    /// <returns>The response body.</returns>
    [SuppressMessage("Refit", "RF001", Justification = "Intentional non-Refit fixture used to verify generator diagnostics.")]
    new Task<string> Test();
}
