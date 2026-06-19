// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;

namespace Refit.Tests;

/// <summary>A request fixture whose query property carries a custom date format.</summary>
public class PathBoundObjectWithQueryFormat
{
    /// <summary>Gets the date value emitted as a custom-formatted query parameter.</summary>
    [Query(Format = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'")]
    public DateTime SomeQueryWithFormat { get; init; }
}
