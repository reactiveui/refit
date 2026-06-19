// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit;

namespace BlazorWasmIssue2065;

/// <summary>Sample Refit API for the issue 2065 reproduction.</summary>
public interface IIssue2065Api
{
    /// <summary>Gets the sample weather payload.</summary>
    /// <returns>The payload content.</returns>
    [Get("/sample-data/weather.json")]
    Task<string> GetPayloadAsync();
}
