// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface that re-declares the non-Refit base members with HTTP attributes.</summary>
public interface IImplementTheInterfaceAndUseRefit : IAmInterfaceEWithNoRefit<int>
{
    /// <summary>Issues a GET request, hiding the non-Refit base member with a Refit-attributed one.</summary>
    /// <param name="parameter">The parameter to send.</param>
    /// <returns>A task representing the request.</returns>
    [Get("/doSomething")]
    public new Task DoSomething(int parameter);

    /// <summary>Issues a GET request, hiding the non-Refit base member with a Refit-attributed one.</summary>
    /// <returns>A task representing the request.</returns>
    [Get("/DoSomethingElse")]
    public new Task DoSomethingElse();
}
