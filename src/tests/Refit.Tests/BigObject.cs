// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A request body fixture carrying a large byte payload, used to exercise buffered serialization.</summary>
public class BigObject
{
    /// <summary>Gets or sets the large binary payload.</summary>
    public byte[]? BigData { get; set; }
}
