// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A plain interface fixture that carries no Refit attributes.</summary>
public interface IInterfaceWithoutRefit
{
    /// <summary>Performs the test operation.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DoSomething();
}
