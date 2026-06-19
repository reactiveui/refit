// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface that inherits from <see cref="IAmInterfaceD"/> to verify deep inheritance.</summary>
[Headers("User-Agent: Refit Integration Tests")]
public interface IAmInterfaceB : IAmInterfaceD
{
    /// <summary>Issues a GET request returning the literal result "Pong".</summary>
    /// <returns>The response body.</returns>
    [Get("/get?result=Pong")]
    Task<string> Pong();
}
