// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// An <see cref="IQueryConverter{T}"/> that flattens a query parameter by walking the <c>JsonTypeInfo</c> of the
/// configured <see cref="SystemTextJsonContentSerializer"/>, reusing a registered <c>JsonSerializerContext</c> so
/// arbitrary (including polymorphic) types flatten without hand-written code. Attach it with
/// <c>[QueryConverter(typeof(SystemTextJsonQueryConverter&lt;MyType&gt;))]</c>.
/// </summary>
/// <typeparam name="T">The declared parameter type.</typeparam>
/// <remarks>
/// Property names come from System.Text.Json (honoring <c>[JsonPropertyName]</c> and the naming policy); values are
/// rendered by <see cref="RefitSettings.UrlParameterFormatter"/>, so enums, dates and numbers match the rest of Refit.
/// The value's runtime type is walked, so a polymorphic value contributes its actual properties. When the configured
/// serializer uses a source-generated <c>TypeInfoResolver</c> the walk is reflection- and AOT-free; otherwise it falls
/// back to System.Text.Json's reflection resolver. Nested objects are flattened under a dotted key; collections use the
/// configured <see cref="RefitSettings.CollectionFormat"/>.
/// </remarks>
public sealed class SystemTextJsonQueryConverter<T> : IQueryConverter<T>
{
    /// <inheritdoc/>
    public void Flatten(T value, string keyPrefix, ref GeneratedQueryStringBuilder builder, RefitSettings settings)
    {
        if (value is null)
        {
            return;
        }

        if (settings.ContentSerializer is not SystemTextJsonContentSerializer serializer)
        {
            throw new NotSupportedException(
                $"SystemTextJsonQueryConverter requires {nameof(RefitSettings)}.{nameof(RefitSettings.ContentSerializer)} to be a {nameof(SystemTextJsonContentSerializer)}.");
        }

        SystemTextJsonQueryFlattener.FlattenObject(value, keyPrefix, ref builder, settings, serializer.SerializerOptions, 0);
    }
}
