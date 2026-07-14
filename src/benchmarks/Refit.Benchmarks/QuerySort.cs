// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Runtime.Serialization;

namespace Refit.Benchmarks;

/// <summary>The sort order used by the query request-building benchmarks.</summary>
public enum QuerySort
{
    /// <summary>Sort by date, newest first.</summary>
    [EnumMember(Value = "date-desc")]
    DateDescending,

    /// <summary>Sort by name.</summary>
    Name,
}
