// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Text.Json;

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
    /// <summary>The recursion cap that bounds a cyclic runtime object graph.</summary>
    private const int MaxDepth = 32;

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

        FlattenObject(value, keyPrefix, ref builder, settings, serializer.SerializerOptions, 0);
    }

    /// <summary>Determines whether a value renders as a single query value rather than a nested object.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns><see langword="true"/> for strings and formattable scalars.</returns>
    private static bool IsSimpleQueryValue(object value) =>
        value is string or bool or char or IFormattable or Uri or System.Globalization.CultureInfo;

    /// <summary>Walks a value's JSON properties and appends each non-null one.</summary>
    /// <param name="value">The object to flatten.</param>
    /// <param name="keyPrefix">The key prefix for this object's properties.</param>
    /// <param name="builder">The query-string builder to append to.</param>
    /// <param name="settings">The active Refit settings.</param>
    /// <param name="options">The serializer options supplying the type metadata.</param>
    /// <param name="depth">The current recursion depth.</param>
    private static void FlattenObject(
        object value,
        string keyPrefix,
        ref GeneratedQueryStringBuilder builder,
        RefitSettings settings,
        JsonSerializerOptions options,
        int depth)
    {
        var properties = options.GetTypeInfo(value.GetType()).Properties;
        for (var i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            if (property.Get is not { } getter)
            {
                continue;
            }

            var propertyValue = getter(value);
            if (propertyValue is not null)
            {
                AppendValue(property.Name, propertyValue, keyPrefix, ref builder, settings, options, depth);
            }
        }
    }

    /// <summary>Appends one property value as a scalar, a collection, or a nested object.</summary>
    /// <param name="name">The JSON property name.</param>
    /// <param name="propertyValue">The non-null property value.</param>
    /// <param name="keyPrefix">The enclosing object's key prefix.</param>
    /// <param name="builder">The query-string builder to append to.</param>
    /// <param name="settings">The active Refit settings.</param>
    /// <param name="options">The serializer options supplying the type metadata.</param>
    /// <param name="depth">The current recursion depth.</param>
    private static void AppendValue(
        string name,
        object propertyValue,
        string keyPrefix,
        ref GeneratedQueryStringBuilder builder,
        RefitSettings settings,
        JsonSerializerOptions options,
        int depth)
    {
        var key = keyPrefix + name;
        if (IsSimpleQueryValue(propertyValue))
        {
            builder.Add(key, Format(propertyValue, settings), false);
            return;
        }

        if (propertyValue is IEnumerable enumerable)
        {
            builder.BeginCollection(key, settings.CollectionFormat, false);
            foreach (var element in enumerable)
            {
                builder.AddCollectionValue(element is null ? null : Format(element, settings));
            }

            builder.EndCollection();
            return;
        }

        if (depth >= MaxDepth)
        {
            return;
        }

        FlattenObject(propertyValue, key + ".", ref builder, settings, options, depth + 1);
    }

    /// <summary>Formats one value through the configured URL parameter formatter.</summary>
    /// <param name="value">The value to format.</param>
    /// <param name="settings">The active Refit settings.</param>
    /// <returns>The formatted value.</returns>
    private static string? Format(object value, RefitSettings settings) =>
        settings.UrlParameterFormatter.Format(value, value.GetType(), value.GetType());
}
