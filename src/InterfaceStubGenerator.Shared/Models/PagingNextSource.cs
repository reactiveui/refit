// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Classifies where a paged method reads the continuation from.</summary>
internal enum PagingNextSource
{
    /// <summary>A member of the page holds the continuation, with the same type as the token.</summary>
    Property = 0,

    /// <summary>A response header holds the continuation.</summary>
    Header = 1,

    /// <summary>A member of the page holds the total item count, from which the next offset is computed.</summary>
    Total = 2,

    /// <summary>A nullable value-type member of the page holds the continuation, and its underlying value is the token.</summary>
    NullableProperty = 3,
}
