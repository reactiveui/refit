// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A minimal Refit client interface used by the inheritance tests.</summary>
public interface IMyClient
{
    /// <summary>Issues a GET request to the root path.</summary>
    /// <param name="ex">A value passed by the test scenario.</param>
    /// <returns>A task representing the request.</returns>
    [Get("/")]
    Task MyMethodAsync(string ex);
}
