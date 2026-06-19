// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A partial Refit interface used to verify members split across multiple files.</summary>
public partial interface IPartialInterfacesApi
{
    /// <summary>Performs a GET request returning the "Second" result.</summary>
    /// <returns>A task whose result is the response body.</returns>
    [Get("/get?result=Second")]
    Task<string> Second();
}
