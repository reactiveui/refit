// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>Transforms a form source from a .NET representation to the appropriate HTTP form encoded representation.</summary>
/// <remarks>Performs field renaming and value formatting as specified in <see cref="QueryAttribute"/>s and
/// <see cref="RefitSettings.FormUrlEncodedParameterFormatter"/>. A given key may appear multiple times with the
/// same or different values.</remarks>
internal sealed class FormValueMultimap : IEnumerable<KeyValuePair<string?, string?>>
{
    /// <summary>The maximum object-graph depth flattened before deeper nested values are dropped, guarding against runaway recursion.</summary>
    private const int MaxNestingDepth = 32;

    /// <summary>The default delimiter composing a nested field key from its parent key, matching the query-flattening default.</summary>
    private const string DefaultNestingDelimiter = ".";

    /// <summary>Caches the per-property attribute metadata for each source type without keeping collectible types alive.</summary>
    /// <remarks>The reflection form path reads <see cref="AliasAsAttribute"/> and <see cref="QueryAttribute"/> for every
    /// property on every request. Those attributes are compile-time metadata that never change for a type, so they are
    /// read once and reused, collapsing the per-property attribute lookups (the dominant allocation on this path) into a
    /// single one-time-per-type cost. Field names still consult the settings' content serializer and key formatter per
    /// call, so settings-dependent naming is never frozen.</remarks>
    private static readonly ConditionalWeakTable<Type, FormPropertyMetadata[]> _metadataCache = new();

    /// <summary>Holds the collected form key/value entries.</summary>
    private readonly List<KeyValuePair<string?, string?>> _formEntries = [];

    /// <summary>The content serializer used to resolve field names.</summary>
    private readonly IHttpContentSerializer _contentSerializer;

    /// <summary>The formatter applied to property names that are not explicitly aliased.</summary>
    private readonly IUrlParameterKeyFormatter _urlParameterKeyFormatter;

    /// <summary>The complex values currently being flattened on the recursion path, guarding against reference cycles.</summary>
    private List<object>? _ancestors;

    /// <summary>Initializes a new instance of the <see cref="FormValueMultimap"/> class from a source object.</summary>
    /// <param name="source">The source object or dictionary to convert into form entries.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    public FormValueMultimap(object source, RefitSettings settings)
        : this(source, settings, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FormValueMultimap"/> class from a source object.</summary>
    /// <param name="source">The source object or dictionary to convert into form entries.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    /// <param name="declaredMetadata">The cached attribute metadata for the declared source type, if available.</param>
    internal FormValueMultimap(
        object? source,
        RefitSettings settings,
        FormPropertyMetadata[]? declaredMetadata)
    {
        ArgumentExceptionHelper.ThrowIfNull(settings);

        _contentSerializer = settings.ContentSerializer;
        _urlParameterKeyFormatter = settings.UrlParameterKeyFormatter;

        if (source is null)
        {
            return;
        }

        if (source is IDictionary dictionary)
        {
            AddDictionary(dictionary, settings, null, DefaultNestingDelimiter, 0);
            return;
        }

        AddObject(source, declaredMetadata ?? GetCachedMetadata(source.GetType()), settings, null, DefaultNestingDelimiter, 0);
    }

    /// <summary>Gets a key for each entry. If multiple entries share the same key, the key is returned multiple times.</summary>
    public IEnumerable<string?> Keys => GetKeys();

    /// <summary>Returns an enumerator over the form key/value entries.</summary>
    /// <returns>An enumerator over the entries.</returns>
    public IEnumerator<KeyValuePair<string?, string?>> GetEnumerator() => _formEntries.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Creates a form value map using the declared source type for property discovery.</summary>
    /// <typeparam name="TSource">The declared source type.</typeparam>
    /// <param name="source">The source object or dictionary to convert into form entries.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    /// <returns>The created form value map.</returns>
    internal static FormValueMultimap Create<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TSource>(
        TSource source,
        RefitSettings settings) =>
        source is null or IDictionary
            ? new(source!, settings)
            : new FormValueMultimap(
                source,
                settings,
                GetCachedMetadata(source));

    /// <summary>Creates a form value map from source-generated field descriptors, avoiding reflection.</summary>
    /// <typeparam name="TBody">The declared body type.</typeparam>
    /// <param name="body">The body instance to flatten into form entries.</param>
    /// <param name="fields">The compile-time field descriptors for the body type.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    /// <returns>The created form value map.</returns>
    internal static FormValueMultimap CreateFromFields<TBody>(
        TBody body,
        FormField<TBody>[] fields,
        RefitSettings settings)
    {
        var map = new FormValueMultimap(null, settings, null);
        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            var value = field.Getter(body);
            var fieldName = field.ResolveFieldName(map._urlParameterKeyFormatter);

            if (value is null)
            {
                if (field.SerializeNull)
                {
                    map.Add(fieldName, string.Empty);
                }

                continue;
            }

            map.AppendValue(fieldName, value, field.Format, field.CollectionFormat, settings, DefaultNestingDelimiter, 0);
        }

        return map;
    }

    /// <summary>Resolves the cached per-property attribute metadata for the given source type.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The cached attribute metadata for each readable public property.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2111:Method with DynamicallyAccessedMembersAttribute is accessed via reflection",
        Justification = "The cache callback receives the same Type key that carries the public property metadata requirement.")]
    internal static FormPropertyMetadata[] GetCachedMetadata(Type type)
        => _metadataCache.GetValue(type, BuildMetadata);

    /// <summary>Resolves the cached per-property attribute metadata for the given declared source type.</summary>
    /// <typeparam name="TSource">The declared source type to inspect.</typeparam>
    /// <param name="source">A value of the declared source type.</param>
    /// <returns>The cached attribute metadata for each readable public property.</returns>
    internal static FormPropertyMetadata[] GetCachedMetadata<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TSource>(TSource source)
    {
        _ = source;

        return _metadataCache.GetValue(typeof(TSource), static _ => BuildMetadata(typeof(TSource)));
    }

    /// <summary>Reads the attribute metadata for each readable public property of the given type.</summary>
    /// <param name="type">The type whose properties are inspected.</param>
    /// <returns>The attribute metadata array, in property order.</returns>
    /// <remarks>Runs once per type as the cache miss callback: the <see cref="AliasAsAttribute"/> name and the
    /// <see cref="QueryAttribute"/> are read here and reused for every subsequent request instead of being re-read per
    /// property per call.</remarks>
    internal static FormPropertyMetadata[] BuildMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type type)
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

    /// <summary>Gets the delimiter string for a delimited collection format.</summary>
    /// <param name="collectionFormat">The delimited collection format.</param>
    /// <returns>The delimiter string.</returns>
    /// <remarks>Mirrors the query-flattening delimiter map: the default <see cref="CollectionFormat.RefitParameterFormatter"/>
    /// and <see cref="CollectionFormat.Csv"/> both join with a comma.</remarks>
    internal static string GetDelimiter(CollectionFormat collectionFormat) =>
        collectionFormat switch
        {
            CollectionFormat.Ssv => " ",
            CollectionFormat.Tsv => "\t",
            CollectionFormat.Pipes => "|",
            _ => ","
        };

    /// <summary>Determines whether a value renders directly through the form formatter instead of being flattened.</summary>
    /// <param name="value">The non-null value to inspect.</param>
    /// <returns><see langword="true"/> for the simple string/formattable types the query-flattening path emits directly.</returns>
    /// <remarks>Mirrors the query path's simple-type predicate: string, bool, char, <see cref="IFormattable"/> (enums,
    /// numbers, dates, <see cref="Guid"/>, ...), <see cref="Uri"/>, and <see cref="CultureInfo"/>.</remarks>
    internal static bool IsSimpleFormValue(object value) =>
        value is string or bool or char or IFormattable or Uri or CultureInfo;

    /// <summary>Composes a form field key from an optional parent prefix and a child name.</summary>
    /// <param name="keyPrefix">The parent key prefix, or <see langword="null"/> at the top level.</param>
    /// <param name="name">The child field name or dictionary key.</param>
    /// <param name="delimiter">The delimiter placed between the prefix and the name.</param>
    /// <returns>The composed key.</returns>
    internal static string? ComposeKey(string? keyPrefix, string? name, string? delimiter) =>
        keyPrefix is null ? name : keyPrefix + delimiter + name;

    /// <summary>Formats and joins a collection-valued form field without LINQ adapters.</summary>
    /// <param name="enumerable">The collection to format.</param>
    /// <param name="delimiter">The delimiter between formatted values.</param>
    /// <param name="format">The value format string, if any.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    /// <returns>The joined formatted value.</returns>
    [SuppressMessage(
        "Correctness",
        "SST2410:A created disposable is never disposed",
        Justification = "ValueStringBuilder.ToString() disposes the builder and returns its pooled buffer; Dispose is idempotent.")]
    internal static string JoinFormattedValues(
        IEnumerable enumerable,
        string delimiter,
        string? format,
        RefitSettings settings)
    {
        var enumerator = enumerable.GetEnumerator();
        try
        {
            if (!enumerator.MoveNext())
            {
                return string.Empty;
            }

            var builder = new ValueStringBuilder(stackalloc char[256]);
            builder.Append(settings.FormUrlEncodedParameterFormatter.Format(enumerator.Current, format));
            while (enumerator.MoveNext())
            {
                builder.Append(delimiter);
                builder.Append(settings.FormUrlEncodedParameterFormatter.Format(enumerator.Current, format));
            }

            return builder.ToString();
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    /// <summary>Adds the entries from an <see cref="IDictionary"/> source, flattening complex entry values.</summary>
    /// <param name="dictionary">The dictionary source.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    /// <param name="keyPrefix">The parent key prefix, or <see langword="null"/> for a top-level dictionary.</param>
    /// <param name="delimiter">The delimiter composing an entry key under the prefix.</param>
    /// <param name="depth">The current nesting depth.</param>
    internal void AddDictionary(
        IDictionary dictionary,
        RefitSettings settings,
        string? keyPrefix,
        string? delimiter,
        int depth)
    {
        foreach (var key in dictionary.Keys)
        {
            var value = dictionary[key];
            if (value is null)
            {
                continue;
            }

            AppendValue(
                ComposeKey(keyPrefix, key.ToString(), delimiter),
                value,
                null,
                null,
                settings,
                delimiter,
                depth);
        }
    }

    /// <summary>Adds the entries reflected from an object source, composing nested keys under the given prefix.</summary>
    /// <param name="source">The object source.</param>
    /// <param name="metadata">The cached attribute metadata for the source's readable public properties.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    /// <param name="keyPrefix">The parent key prefix, or <see langword="null"/> for a top-level object.</param>
    /// <param name="delimiter">The delimiter composing each property key under the prefix.</param>
    /// <param name="depth">The current nesting depth.</param>
    internal void AddObject(
        object source,
        FormPropertyMetadata[] metadata,
        RefitSettings settings,
        string? keyPrefix,
        string? delimiter,
        int depth)
    {
        for (var i = 0; i < metadata.Length; i++)
        {
            var property = metadata[i].Property;
            var value = property.GetValue(source, null);

            // The query attribute was read once when the metadata was cached for this type.
            var attrib = metadata[i].Query;

            var fieldName = ComposeKey(keyPrefix, GetFieldNameForProperty(metadata[i]), delimiter);

            if (value is null)
            {
                if (attrib?.SerializeNull == true)
                {
                    Add(fieldName, string.Empty);
                }

                continue;
            }

            var collectionFormat = attrib?.IsCollectionFormatSpecified == true
                ? attrib.CollectionFormat
                : (CollectionFormat?)null;

            // A nested object/dictionary composes its children under this property's key using this property's own
            // [Query] delimiter when present, otherwise the inherited delimiter (matching the query-flattening path).
            var nestedDelimiter = attrib is not null ? attrib.Delimiter : (delimiter ?? DefaultNestingDelimiter);

            AppendValue(fieldName, value, attrib?.Format, collectionFormat, settings, nestedDelimiter, depth);
        }
    }

    /// <summary>Adds a non-null property or descriptor value, choosing scalar, collection, dictionary, or nested-object handling.</summary>
    /// <param name="fieldName">The resolved form field name.</param>
    /// <param name="value">The non-null value to add.</param>
    /// <param name="format">The value format string, if any.</param>
    /// <param name="explicitCollectionFormat">The explicit collection format, or <see langword="null"/> to use the settings default.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    /// <param name="delimiter">The delimiter composing nested keys under this field.</param>
    /// <param name="depth">The current nesting depth.</param>
    internal void AppendValue(
        string? fieldName,
        object value,
        string? format,
        CollectionFormat? explicitCollectionFormat,
        RefitSettings settings,
        string? delimiter,
        int depth)
    {
        // Simple scalar and formattable values (including strings) render directly through the form formatter.
        if (IsSimpleFormValue(value))
        {
            Add(
                fieldName,
                settings.FormUrlEncodedParameterFormatter.Format(value, format));
            return;
        }

        // A dictionary flattens to "field<delimiter>key" entries, recursing into complex entry values.
        if (value is IDictionary dictionary)
        {
            if (TryEnterComplex(value, depth))
            {
                AddDictionary(dictionary, settings, fieldName, delimiter, depth + 1);
                ExitComplex();
            }

            return;
        }

        // A collection formats and joins (or repeats) its elements per the resolved collection format.
        if (value is IEnumerable enumerable)
        {
            var collectionFormat = explicitCollectionFormat ?? settings.CollectionFormat;
            AddCollection(fieldName, enumerable, value, format, collectionFormat, settings);
            return;
        }

        // A nested complex object flattens its public properties under "field<delimiter>child".
        if (!TryEnterComplex(value, depth))
        {
            return;
        }

        AddObject(value, GetCachedMetadata(value.GetType()), settings, fieldName, delimiter, depth + 1);
        ExitComplex();
    }

    /// <summary>Enters a complex value on the recursion path, enforcing the depth cap and reference-cycle guard.</summary>
    /// <param name="value">The complex value about to be flattened.</param>
    /// <param name="depth">The current nesting depth.</param>
    /// <returns><see langword="true"/> when flattening may proceed; <see langword="false"/> when the value is too deep
    /// or already being flattened on this path, in which case it is dropped.</returns>
    internal bool TryEnterComplex(object value, int depth)
    {
        if (depth >= MaxNestingDepth)
        {
            return false;
        }

        _ancestors ??= [];
        for (var i = 0; i < _ancestors.Count; i++)
        {
            if (ReferenceEquals(_ancestors[i], value))
            {
                return false;
            }
        }

        _ancestors.Add(value);
        return true;
    }

    /// <summary>Leaves the most recently entered complex value, restoring the recursion path for the next sibling.</summary>
    internal void ExitComplex() =>

        // _ancestors is non-null here: TryEnterComplex allocated it and pushed the matching value.
        _ancestors!.RemoveAt(_ancestors.Count - 1);

    /// <summary>Adds a collection-valued property using the resolved collection format.</summary>
    /// <param name="fieldName">The resolved form field name.</param>
    /// <param name="enumerable">The enumerable value.</param>
    /// <param name="value">The original property value.</param>
    /// <param name="format">The value format string, if any.</param>
    /// <param name="collectionFormat">The resolved collection format.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    internal void AddCollection(
        string? fieldName,
        IEnumerable enumerable,
        object value,
        string? format,
        CollectionFormat collectionFormat,
        RefitSettings settings)
    {
        switch (collectionFormat)
        {
            case CollectionFormat.Multi:
            {
                foreach (var item in enumerable)
                {
                    Add(
                        fieldName,
                        settings.FormUrlEncodedParameterFormatter.Format(
                            item,
                            format));
                }

                break;
            }

            case CollectionFormat.RefitParameterFormatter
                or CollectionFormat.Csv
                or CollectionFormat.Ssv
                or CollectionFormat.Tsv
                or CollectionFormat.Pipes:
            {
                var delimiter = GetDelimiter(collectionFormat);

                Add(fieldName, JoinFormattedValues(enumerable, delimiter, format, settings));
                break;
            }

            default:
            {
                // A genuinely out-of-range CollectionFormat value falls back to formatting the collection object itself.
                Add(
                    fieldName,
                    settings.FormUrlEncodedParameterFormatter.Format(
                        value,
                        format));
                break;
            }
        }
    }

    /// <summary>Adds a key/value pair to the form entries.</summary>
    /// <param name="key">The form field key.</param>
    /// <param name="value">The form field value.</param>
    internal void Add(string? key, string? value) => _formEntries.Add(new(key, value));

    /// <summary>Returns each key from the collected form entries.</summary>
    /// <returns>The form keys.</returns>
    internal IEnumerable<string?> GetKeys()
    {
        for (var i = 0; i < _formEntries.Count; i++)
        {
            yield return _formEntries[i].Key;
        }
    }

    /// <summary>Resolves the form field name for the given property from its cached attribute metadata.</summary>
    /// <param name="metadata">The cached attribute metadata for the property.</param>
    /// <returns>The resolved form field name.</returns>
    /// <remarks>The <see cref="AliasAsAttribute"/> name and <see cref="QueryAttribute"/> come from the per-type cache;
    /// the content serializer and key formatter are still consulted per call because they are settings-dependent.</remarks>
    internal string GetFieldNameForProperty(in FormPropertyMetadata metadata)
    {
        var name = metadata.AliasName
                   ?? _contentSerializer.GetFieldNameForProperty(metadata.Property)
                   ?? _urlParameterKeyFormatter.Format(metadata.Property.Name);

        var qattrib = metadata.Query;
        return qattrib is not null && !string.IsNullOrWhiteSpace(qattrib.Prefix)
            ? qattrib.Prefix + qattrib.Delimiter + name
            : name;
    }

    /// <summary>The cached, settings-independent attribute metadata for a single readable public property on a form
    /// source type, read once when the property list is first cached and reused for every request.</summary>
    /// <param name="Property">The reflected property.</param>
    /// <param name="AliasName">The <see cref="AliasAsAttribute.Name"/> override, or <see langword="null"/> when absent.</param>
    /// <param name="Query">The <see cref="QueryAttribute"/> applied to the property, or <see langword="null"/> when absent.</param>
    internal readonly record struct FormPropertyMetadata(
        PropertyInfo Property,
        string? AliasName,
        QueryAttribute? Query);
}
