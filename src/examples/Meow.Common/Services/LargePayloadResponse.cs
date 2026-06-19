// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Newtonsoft.Json;

namespace Meow;

/// <summary>Response carrying a large list of items.</summary>
public sealed class LargePayloadResponse
{
    /// <summary>Gets the list of items in the payload.</summary>
    [JsonProperty("items")]
    public List<int> Items { get; } = [];
}
