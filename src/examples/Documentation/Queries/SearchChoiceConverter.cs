// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Documentation;

/// <summary>Flattens a search choice into q and limit rather than its CLR property names.</summary>
internal sealed class SearchChoiceConverter : IQueryConverter<SearchChoice>
{
    /// <summary>Appends escaped search text and an invariant limit under the supplied prefix.</summary>
    /// <param name="value">The source value consumed by the converter or formatter.</param>
    /// <param name="keyPrefix">The query prefix applied to both custom field names.</param>
    /// <param name="builder">The request's query builder, updated with the flattened fields.</param>
    /// <param name="settings">The caller's query configuration; this converter uses invariant limit formatting.</param>
    public void Flatten(SearchChoice value, string keyPrefix, ref GeneratedQueryStringBuilder builder, RefitSettings settings)
    {
        builder.Add($"{keyPrefix}q", value.Name, false);
        builder.Add($"{keyPrefix}limit", GeneratedRequestRunner.FormatInvariant(value.Limit, null), false);
    }
}
