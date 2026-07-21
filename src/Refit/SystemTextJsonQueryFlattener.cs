// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Text.Json;

namespace Refit;

/// <summary>
/// The type-parameter-independent flattening engine behind <see cref="SystemTextJsonQueryConverter{T}"/>. Kept
/// non-generic so a single copy of the walk is shared across every closed converter instantiation.
/// </summary>
internal static class SystemTextJsonQueryFlattener
{
    /// <summary>The recursion cap that bounds a cyclic runtime object graph.</summary>
    private const int MaxDepth = 32;

    /// <summary>Determines whether a value renders as a single query value rather than a nested object.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns><see langword="true"/> for strings and formattable scalars.</returns>
    internal static bool IsSimpleQueryValue(object value) =>
        value is string or bool or char or IFormattable or Uri or System.Globalization.CultureInfo;

    /// <summary>Walks a value's JSON properties and appends each non-null one.</summary>
    /// <param name="value">The object to flatten.</param>
    /// <param name="keyPrefix">The key prefix for this object's properties.</param>
    /// <param name="builder">The query-string builder to append to.</param>
    /// <param name="settings">The active Refit settings.</param>
    /// <param name="options">The serializer options supplying the type metadata.</param>
    /// <param name="depth">The current recursion depth.</param>
    internal static void FlattenObject(
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
    internal static void AppendValue(
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

        FlattenObject(propertyValue, $"{key}.", ref builder, settings, options, depth + 1);
    }

    /// <summary>Formats one value through the configured URL parameter formatter.</summary>
    /// <param name="value">The value to format.</param>
    /// <param name="settings">The active Refit settings.</param>
    /// <returns>The formatted value.</returns>
    internal static string? Format(object value, RefitSettings settings) =>
        settings.UrlParameterFormatter.Format(value, value.GetType(), value.GetType());
}
