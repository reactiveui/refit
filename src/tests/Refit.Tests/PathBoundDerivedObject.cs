// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A derived request fixture that adds a further path-bound property.</summary>
public class PathBoundDerivedObject : PathBoundObject
{
    /// <summary>Gets the third property bound to the path.</summary>
    public string? SomeProperty3 { get; init; }
}
