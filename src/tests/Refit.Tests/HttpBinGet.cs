// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Refit.Tests;

/// <summary>A deserialization fixture mirroring the httpbin echo response.</summary>
public class HttpBinGet
{
    /// <summary>Gets the echoed query arguments.</summary>
    public Dictionary<string, object>? Args { get; init; }

    /// <summary>Gets the echoed request headers.</summary>
    public Dictionary<string, string>? Headers { get; init; }

    /// <summary>Gets the request origin.</summary>
    public string? Origin { get; init; }

    /// <summary>Gets the request URL.</summary>
    public string? Url { get; init; }
}
