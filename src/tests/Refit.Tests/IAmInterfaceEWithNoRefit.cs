// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A deliberately non-Refit generic interface used to verify the generator ignores interfaces without HTTP attributes.</summary>
/// <typeparam name="T">The parameter type accepted by <see cref="DoSomething"/>.</typeparam>
public interface IAmInterfaceEWithNoRefit<in T>
{
    /// <summary>A non-Refit method accepting a parameter; intentionally has no HTTP attribute.</summary>
    /// <param name="parameter">An arbitrary parameter.</param>
    /// <returns>A task representing the operation.</returns>
    [SuppressMessage("Refit", "RF001", Justification = "Intentional non-Refit fixture used to verify generator diagnostics.")]
    Task DoSomething(T parameter);

    /// <summary>A non-Refit method; intentionally has no HTTP attribute.</summary>
    /// <returns>A task representing the operation.</returns>
    [SuppressMessage("Refit", "RF001", Justification = "Intentional non-Refit fixture used to verify generator diagnostics.")]
    Task DoSomethingElse();
}
