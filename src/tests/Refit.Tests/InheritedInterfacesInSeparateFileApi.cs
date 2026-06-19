// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;

namespace Refit.Tests.SeparateNamespace;

/// <summary>A Refit fixture interface that inherits from an interface declared in a separate file.</summary>
public interface InheritedInterfacesInSeparateFileApi : IAmInterfaceF_RequireUsing
{
    /// <summary>Sends a GET request to the test endpoint.</summary>
    /// <param name="i">A test value supplied by the caller.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/get")]
    Task Get(int i);
}
