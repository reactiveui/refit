// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>Flattens complex form values by reading their public properties through reflection.</summary>
internal sealed class FormObjectFlattener : IFormObjectFlattener
{
    /// <summary>Caches the per-property attribute metadata for each source type without keeping collectible types alive.</summary>
    /// <remarks>The attributes are compile-time metadata that never change for a type, so they are read once and reused.
    /// Field names still consult the settings' content serializer and key formatter per call, so settings-dependent
    /// naming is never frozen.</remarks>
    private static readonly ConditionalWeakTable<Type, FormPropertyMetadata[]> _metadataCache = new();

    /// <inheritdoc/>
    public void AddObject(
        FormValueMultimap map,
        object source,
        Type sourceType,
        RefitSettings settings,
        string? keyPrefix,
        string? delimiter,
        int depth)
    {
        var metadata = _metadataCache.GetValue(sourceType, BuildMetadata);
        for (var i = 0; i < metadata.Length; i++)
        {
            var value = metadata[i].Property.GetValue(source, null);

            // The query attribute was read once when the metadata was cached for this type.
            var attrib = metadata[i].Query;

            var fieldName = FormValueMultimap.ComposeKey(keyPrefix, GetFieldNameForProperty(map, metadata[i]), delimiter);

            if (value is null)
            {
                if (attrib?.SerializeNull == true)
                {
                    map.Add(fieldName, string.Empty);
                }

                continue;
            }

            var collectionFormat = attrib?.IsCollectionFormatSpecified == true
                ? attrib.CollectionFormat
                : (CollectionFormat?)null;

            // A nested object/dictionary composes its children under this property's key using this property's own
            // [Query] delimiter when present, otherwise the inherited delimiter (matching the query-flattening path).
            var nestedDelimiter = attrib is not null ? attrib.Delimiter : (delimiter ?? FormValueMultimap.DefaultNestingDelimiter);

            map.AppendValue(fieldName, value, attrib?.Format, collectionFormat, settings, nestedDelimiter, depth);
        }
    }

    /// <summary>Reads the attribute metadata for each readable public property of the given type.</summary>
    /// <param name="type">The type whose properties are inspected.</param>
    /// <returns>The attribute metadata array, in property order.</returns>
    internal static FormPropertyMetadata[] BuildMetadata(Type type)
    {
        var properties = ReflectionPropertyHelpers.GetReadablePublicInstanceProperties(type);
        var metadata = new FormPropertyMetadata[properties.Length];
        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            metadata[i] = new(
                property,
                property.GetCustomAttribute<AliasAsAttribute>(true)?.Name,
                property.GetCustomAttribute<QueryAttribute>(true));
        }

        return metadata;
    }

    /// <summary>Resolves the form field name for the given property from its cached attribute metadata.</summary>
    /// <param name="map">The form map whose settings supply the content serializer and key formatter.</param>
    /// <param name="metadata">The cached attribute metadata for the property.</param>
    /// <returns>The resolved form field name.</returns>
    internal static string GetFieldNameForProperty(FormValueMultimap map, in FormPropertyMetadata metadata)
    {
        var name = metadata.AliasName
                   ?? metadata.Query?.Name
                   ?? SerializerFieldNames.Resolve(map.ContentSerializer, metadata.Property)
                   ?? map.UrlParameterKeyFormatter.Format(metadata.Property.Name);

        var qattrib = metadata.Query;
        return qattrib is not null && !string.IsNullOrWhiteSpace(qattrib.Prefix)
            ? qattrib.Prefix + qattrib.Delimiter + name
            : name;
    }
}
