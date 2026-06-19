// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface used to verify trailing-slash handling on the base URL.</summary>
public interface ITrimTrailingForwardSlashApi
{
    /// <summary>Gets the underlying HTTP client.</summary>
    HttpClient Client { get; }

    /// <summary>Gets a fixed endpoint.</summary>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/someendpoint")]
    Task Get();
}
