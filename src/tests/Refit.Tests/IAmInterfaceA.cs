// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A base Refit interface used to verify inherited members.</summary>
[Headers("User-Agent: Refit Integration Tests")]
public interface IAmInterfaceA
{
    /// <summary>Issues a GET request returning the literal result "Ping".</summary>
    /// <returns>The response body.</returns>
    [Get("/get?result=Ping")]
    Task<string> Ping();
}
