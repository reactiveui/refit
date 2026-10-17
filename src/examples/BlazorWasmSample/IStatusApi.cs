// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit;

namespace BlazorWasmSample;

/// <summary>Sample Refit API for fetching a typed status payload in Blazor WebAssembly.</summary>
internal interface IStatusApi
{
    /// <summary>Gets the status from the sample data endpoint.</summary>
    /// <returns>The status response.</returns>
    [Get("/sample-data/status.json")]
    Task<SampleStatusResponse> GetStatusAsync();
}
