// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Newtonsoft.Json;

namespace Meow;

/// <summary>Response carrying the echoed customer id header.</summary>
public sealed class CustomerEchoResponse
{
    /// <summary>Gets or sets the customer id header value echoed by the backend.</summary>
    [JsonProperty("customerIdHeader")]
    public string? CustomerIdHeader { get; set; }
}
