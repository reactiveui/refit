// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Refit.Tests;

/// <summary>The result of a GitHub user search, used as a deserialization fixture in the Refit tests.</summary>
public class UserSearchResult
{
    /// <summary>Gets or sets the total number of matching users.</summary>
    public int TotalCount { get; init; }

    /// <summary>Gets or sets a value indicating whether the results are incomplete.</summary>
    public bool IncompleteResults { get; init; }

    /// <summary>Gets the matching users for this page of results.</summary>
    public IList<User>? Items { get; init; }
}
