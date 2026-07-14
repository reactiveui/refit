// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>Transforms a form source from a .NET representation to the appropriate HTTP form encoded representation.</summary>
/// <remarks>Performs field renaming and value formatting as specified in <see cref="QueryAttribute"/>s and
/// <see cref="RefitSettings.FormUrlEncodedParameterFormatter"/>. A given key may appear multiple times with the
/// same or different values.</remarks>
internal sealed class FormValueMultimap : IEnumerable<KeyValuePair<string?, string?>>
{
    /// <summary>Caches the readable public properties for each source type without keeping collectible types alive.</summary>
    private static readonly ConditionalWeakTable<Type, PropertyInfo[]> _propertyCache = new();

    /// <summary>Holds the collected form key/value entries.</summary>
    private readonly List<KeyValuePair<string?, string?>> _formEntries = [];

    /// <summary>The content serializer used to resolve field names.</summary>
    private readonly IHttpContentSerializer _contentSerializer;

    /// <summary>The formatter applied to property names that are not explicitly aliased.</summary>
    private readonly IUrlParameterKeyFormatter _urlParameterKeyFormatter;

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
    /// <param name="declaredProperties">The declared source type properties, if available.</param>
    private FormValueMultimap(
        object? source,
        RefitSettings settings,
        PropertyInfo[]? declaredProperties)
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
            AddDictionary(dictionary, settings);
            return;
        }

        AddObject(source, declaredProperties ?? GetCachedProperties(source.GetType()), settings);
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
                GetCachedProperties(source));

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

            map.AppendValue(fieldName, value, field.Format, field.CollectionFormat, settings);
        }

        return map;
    }

    /// <summary>Resolves the cached readable public properties for the given source type.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The cached readable public properties.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2111:Method with DynamicallyAccessedMembersAttribute is accessed via reflection",
        Justification = "The cache callback receives the same Type key that carries the public property metadata requirement.")]
    private static PropertyInfo[] GetCachedProperties(Type type)
        => _propertyCache.GetValue(type, ReflectionPropertyHelpers.GetReadablePublicInstanceProperties);

    /// <summary>Resolves the cached readable public properties for the given declared source type.</summary>
    /// <typeparam name="TSource">The declared source type to inspect.</typeparam>
    /// <param name="source">A value of the declared source type.</param>
    /// <returns>The cached readable public properties.</returns>
    private static PropertyInfo[] GetCachedProperties<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TSource>(TSource source)
    {
        _ = source;

        return _propertyCache.GetValue(
            typeof(TSource),
            static _ => ReflectionPropertyHelpers.GetReadablePublicInstanceProperties(typeof(TSource)));
    }

    /// <summary>Gets the delimiter string for a delimited collection format.</summary>
    /// <param name="collectionFormat">The delimited collection format.</param>
    /// <returns>The delimiter string.</returns>
    private static string GetDelimiter(CollectionFormat collectionFormat) =>
        collectionFormat switch
        {
            CollectionFormat.Csv => ",",
            CollectionFormat.Ssv => " ",
            CollectionFormat.Tsv => "\t",
            _ => "|"
        };

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
    private static string JoinFormattedValues(
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

    /// <summary>Adds the entries from an <see cref="IDictionary"/> source.</summary>
    /// <param name="dictionary">The dictionary source.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    private void AddDictionary(IDictionary dictionary, RefitSettings settings)
    {
        foreach (var key in dictionary.Keys)
        {
            var value = dictionary[key];
            if (value is not null)
            {
                Add(
                    key.ToString(),
                    settings.FormUrlEncodedParameterFormatter.Format(value, null));
            }
        }
    }

    /// <summary>Adds the entries reflected from an object source.</summary>
    /// <param name="source">The object source.</param>
    /// <param name="properties">The properties to read from the source.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    private void AddObject(
        object source,
        PropertyInfo[] properties,
        RefitSettings settings)
    {
        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            var value = property.GetValue(source, null);

            // see if there's a query attribute
            var attrib = property.GetCustomAttribute<QueryAttribute>(true);

            if (value is null)
            {
                if (attrib?.SerializeNull == true)
                {
                    Add(GetFieldNameForProperty(property), string.Empty);
                }

                continue;
            }

            var fieldName = GetFieldNameForProperty(property);
            var collectionFormat = attrib?.IsCollectionFormatSpecified == true
                ? attrib.CollectionFormat
                : (CollectionFormat?)null;

            AppendValue(fieldName, value, attrib?.Format, collectionFormat, settings);
        }
    }

    /// <summary>Adds a non-null property or descriptor value, choosing scalar or collection handling.</summary>
    /// <param name="fieldName">The resolved form field name.</param>
    /// <param name="value">The non-null value to add.</param>
    /// <param name="format">The value format string, if any.</param>
    /// <param name="explicitCollectionFormat">The explicit collection format, or <see langword="null"/> to use the settings default.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    private void AppendValue(
        string? fieldName,
        object value,
        string? format,
        CollectionFormat? explicitCollectionFormat,
        RefitSettings settings)
    {
        // add strings/non enumerable properties
        if (value is not IEnumerable enumerable || value is string)
        {
            Add(
                fieldName,
                settings.FormUrlEncodedParameterFormatter.Format(value, format));
            return;
        }

        var collectionFormat = explicitCollectionFormat ?? settings.CollectionFormat;
        AddCollection(fieldName, enumerable, value, format, collectionFormat, settings);
    }

    /// <summary>Adds a collection-valued property using the resolved collection format.</summary>
    /// <param name="fieldName">The resolved form field name.</param>
    /// <param name="enumerable">The enumerable value.</param>
    /// <param name="value">The original property value.</param>
    /// <param name="format">The value format string, if any.</param>
    /// <param name="collectionFormat">The resolved collection format.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    private void AddCollection(
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

            case CollectionFormat.Csv
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
    private void Add(string? key, string? value) => _formEntries.Add(new(key, value));

    /// <summary>Returns each key from the collected form entries.</summary>
    /// <returns>The form keys.</returns>
    private IEnumerable<string?> GetKeys()
    {
        for (var i = 0; i < _formEntries.Count; i++)
        {
            yield return _formEntries[i].Key;
        }
    }

    /// <summary>Resolves the form field name for the given property.</summary>
    /// <param name="propertyInfo">The property to resolve the name for.</param>
    /// <returns>The resolved form field name.</returns>
    private string GetFieldNameForProperty(PropertyInfo propertyInfo)
    {
        var name = propertyInfo.GetCustomAttribute<AliasAsAttribute>(true)?.Name
                   ?? _contentSerializer.GetFieldNameForProperty(propertyInfo)
                   ?? _urlParameterKeyFormatter.Format(propertyInfo.Name);

        var qattrib = propertyInfo.GetCustomAttribute<QueryAttribute>(true);
        return qattrib is not null && !string.IsNullOrWhiteSpace(qattrib.Prefix)
            ? qattrib.Prefix + qattrib.Delimiter + name
            : name;
    }
}
