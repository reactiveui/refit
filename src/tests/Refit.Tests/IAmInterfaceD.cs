// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A leaf Refit interface inherited by other interfaces in the inheritance tests.</summary>
public interface IAmInterfaceD
{
    /// <summary>Issues a GET request returning the literal result "Test".</summary>
    /// <returns>The response body.</returns>
    [Get("/get?result=Test")]
    Task<string> Test();
}
