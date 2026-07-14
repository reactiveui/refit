// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A request object whose identifier property binds to a multipart route path.</summary>
public sealed class MultipartRoutingRequest
{
    /// <summary>Gets or sets the identifier bound to the route path.</summary>
    public string? Id { get; set; }
}
