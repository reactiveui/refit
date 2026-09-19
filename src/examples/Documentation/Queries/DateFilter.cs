// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Documentation;

/// <summary>Demonstrates containing-type and property-level date format selection.</summary>
internal sealed class DateFilter
{
    /// <summary>Gets the date formatted using the DateFilter-specific registration.</summary>
    public DateTime Started { get; init; }

    /// <summary>Gets the date whose explicit year format overrides registered formats.</summary>
    [Query(Format = "yyyy")]
    public DateTime End { get; init; }
}
