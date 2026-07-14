// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// Flattens a query parameter value into query-string pairs by hand, for shapes the source generator cannot flatten
/// from the declared type (an <see cref="object"/> value, a polymorphic base type, a <c>Dictionary&lt;string, object&gt;</c>,
/// and similar). Attach an implementation to a parameter with <see cref="QueryConverterAttribute"/>.
/// </summary>
/// <typeparam name="T">The declared parameter type the converter handles.</typeparam>
/// <remarks>
/// A converter is a source-generation-only feature: it lets an otherwise-unflattenable parameter generate inline,
/// writing directly into the pooled <see cref="GeneratedQueryStringBuilder"/> so the path stays reflection- and
/// allocation-free. It is not consulted by the reflection request builder, which walks the value's runtime type
/// instead. Implementations should be stateless; the generator caches a single instance per converter type.
/// </remarks>
public interface IQueryConverter<in T>
{
    /// <summary>Writes the query pairs for <paramref name="value"/> into <paramref name="builder"/>.</summary>
    /// <param name="value">The parameter value; never null when the parameter is null-guarded by the generator.</param>
    /// <param name="keyPrefix">The resolved key prefix from the parameter's <c>[Query(Prefix)]</c>, or an empty
    /// string. Prepend it to each key you write.</param>
    /// <param name="builder">The query-string builder to append pairs to (via <see cref="GeneratedQueryStringBuilder.Add"/>).</param>
    /// <param name="settings">The active Refit settings, exposing the configured formatters.</param>
    void Flatten(T value, string keyPrefix, ref GeneratedQueryStringBuilder builder, RefitSettings settings);
}
