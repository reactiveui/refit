// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A small record used to exercise JSON Lines request bodies.</summary>
public class JsonLineRecord
{
    /// <summary>Gets or sets the identifier.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string? Name { get; set; }
}
