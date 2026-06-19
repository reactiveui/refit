// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit;

namespace BlazorWasmIssue2065;

/// <summary>Sample Refit API used to reproduce issue 2067.</summary>
internal interface IIssue2067Api
{
    /// <summary>Gets the status from the sample data endpoint.</summary>
    /// <returns>The status response.</returns>
    [Get("/sample-data/status.json")]
    Task<Issue2067Response> GetStatusAsync();
}
