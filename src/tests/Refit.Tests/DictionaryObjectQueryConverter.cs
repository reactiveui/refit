// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Flattens an <c>object</c>-valued dictionary — a shape the generator cannot flatten from the declared
/// type — into one query pair per non-null entry, demonstrating the <see cref="IQueryConverter{T}"/> escape hatch.</summary>
public sealed class DictionaryObjectQueryConverter : IQueryConverter<IDictionary<string, object>>
{
    /// <inheritdoc/>
    public void Flatten(
        IDictionary<string, object> value,
        string keyPrefix,
        ref GeneratedQueryStringBuilder builder,
        RefitSettings settings)
    {
        foreach (var entry in value)
        {
            if (entry.Value is null)
            {
                continue;
            }

            var formatted = settings.UrlParameterFormatter.Format(entry.Value, typeof(object), typeof(object));
            builder.Add(keyPrefix + entry.Key, formatted, false);
        }
    }
}
