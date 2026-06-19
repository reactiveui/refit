// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Refit.Tests;

/// <summary>A request fixture whose list property is bound to a path segment.</summary>
public class PathBoundList
{
    /// <summary>Gets the list of values bound to the path.</summary>
    public List<int>? Values { get; init; }
}
