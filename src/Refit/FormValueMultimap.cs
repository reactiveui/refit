// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Refit;

/// <summary>Transforms a form source from a .NET representation to the appropriate HTTP form encoded representation.</summary>
/// <remarks>Performs field renaming and value formatting as specified in <see cref="QueryAttribute"/>s and
/// <see cref="RefitSettings.FormUrlEncodedParameterFormatter"/>. A given key may appear multiple times with the
/// same or different values.</remarks>
internal sealed class FormValueMultimap : IEnumerable<KeyValuePair<string?, string?>>
{
    /// <summary>Guards access to the shared property cache.</summary>
#if NET9_0_OR_GREATER
    private static readonly Lock _lock = new();
#else
    private static readonly object _lock = new();
#endif

    /// <summary>Caches the readable public properties for each source type.</summary>
    private static readonly Dictionary<Type, PropertyInfo[]> _propertyCache = [];

    /// <summary>Holds the collected form key/value entries.</summary>
    private readonly List<KeyValuePair<string?, string?>> _formEntries = [];

    /// <summary>The content serializer used to resolve field names.</summary>
    private readonly IHttpContentSerializer _contentSerializer;

    /// <summary>Initializes a new instance of the <see cref="FormValueMultimap"/> class from a source object.</summary>
    /// <param name="source">The source object or dictionary to convert into form entries.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    [RequiresUnreferencedCode(
        "Form URL encoded bodies reflect over runtime object properties and serializer metadata.")]
    public FormValueMultimap(object source, RefitSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        _contentSerializer = settings.ContentSerializer;

        if (source is null)
        {
            return;
        }

        if (source is IDictionary dictionary)
        {
            AddDictionary(dictionary, settings);
            return;
        }

        AddObject(source, settings);
    }

    /// <summary>Gets a key for each entry. If multiple entries share the same key, the key is returned multiple times.</summary>
    public IEnumerable<string?> Keys => this.Select(it => it.Key);

    /// <summary>Returns an enumerator over the form key/value entries.</summary>
    /// <returns>An enumerator over the entries.</returns>
    public IEnumerator<KeyValuePair<string?, string?>> GetEnumerator() => _formEntries.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Gets the readable public instance properties of the given type.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The readable public properties.</returns>
    private static PropertyInfo[] GetProperties(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type type) =>
    [
        .. type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.GetMethod?.IsPublic == true)];

    /// <summary>Resolves the cached readable public properties for the given source type.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The cached readable public properties.</returns>
    [RequiresUnreferencedCode(
        "Form URL encoded bodies reflect over runtime object properties and serializer metadata.")]
    private static PropertyInfo[] GetCachedProperties(Type type)
    {
        lock (_lock)
        {
            if (!_propertyCache.TryGetValue(type, out var properties))
            {
                properties = GetProperties(type);
                _propertyCache[type] = properties;
            }

            return properties;
        }
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

    /// <summary>Adds the entries from an <see cref="IDictionary"/> source.</summary>
    /// <param name="dictionary">The dictionary source.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    [RequiresUnreferencedCode(
        "Form URL encoded value formatting may reflect over runtime enum metadata; use the Refit source generator for trimmed/AOT apps.")]
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
    /// <param name="settings">The Refit settings controlling formatting.</param>
    [RequiresUnreferencedCode(
        "Form URL encoded bodies reflect over runtime object properties and serializer metadata.")]
    private void AddObject(object source, RefitSettings settings)
    {
        foreach (var property in GetCachedProperties(source.GetType()))
        {
            var value = property.GetValue(source, null);
            if (value is null)
            {
                continue;
            }

            var fieldName = GetFieldNameForProperty(property);

            // see if there's a query attribute
            var attrib = property.GetCustomAttribute<QueryAttribute>(true);

            // add strings/non enumerable properties
            if (value is not IEnumerable enumerable || value is string)
            {
                Add(
                    fieldName,
                    settings.FormUrlEncodedParameterFormatter.Format(value, attrib?.Format));
                continue;
            }

            var collectionFormat =
                attrib?.IsCollectionFormatSpecified == true
                    ? attrib.CollectionFormat
                    : settings.CollectionFormat;

            AddCollection(fieldName, enumerable, value, attrib, collectionFormat, settings);
        }
    }

    /// <summary>Adds a collection-valued property using the resolved collection format.</summary>
    /// <param name="fieldName">The resolved form field name.</param>
    /// <param name="enumerable">The enumerable value.</param>
    /// <param name="value">The original property value.</param>
    /// <param name="attrib">The query attribute, if any.</param>
    /// <param name="collectionFormat">The resolved collection format.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    [RequiresUnreferencedCode(
        "Form URL encoded value formatting may reflect over runtime enum metadata; use the Refit source generator for trimmed/AOT apps.")]
    private void AddCollection(
        string fieldName,
        IEnumerable enumerable,
        object value,
        QueryAttribute? attrib,
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
                            attrib?.Format));
                }

                break;
            }

            case CollectionFormat.Csv
                or CollectionFormat.Ssv
                or CollectionFormat.Tsv
                or CollectionFormat.Pipes:
            {
                var delimiter = GetDelimiter(collectionFormat);

                var formattedValues = enumerable
                    .Cast<object>()
                    .Select(v =>
                        settings.FormUrlEncodedParameterFormatter.Format(
                            v,
                            attrib?.Format));
                Add(fieldName, string.Join(delimiter, formattedValues));
                break;
            }

            default:
            {
                Add(
                    fieldName,
                    settings.FormUrlEncodedParameterFormatter.Format(
                        value,
                        attrib?.Format));
                break;
            }
        }
    }

    /// <summary>Adds a key/value pair to the form entries.</summary>
    /// <param name="key">The form field key.</param>
    /// <param name="value">The form field value.</param>
    private void Add(string? key, string? value) => _formEntries.Add(new(key, value));

    /// <summary>Resolves the form field name for the given property.</summary>
    /// <param name="propertyInfo">The property to resolve the name for.</param>
    /// <returns>The resolved form field name.</returns>
    private string GetFieldNameForProperty(PropertyInfo propertyInfo)
    {
        var name =
            propertyInfo
                .GetCustomAttributes<AliasAsAttribute>(true)
                .Select(a => a.Name)
                .FirstOrDefault()
            ?? _contentSerializer.GetFieldNameForProperty(propertyInfo)
            ?? propertyInfo.Name;

        var qattrib = propertyInfo
            .GetCustomAttributes<QueryAttribute>(true)
            .Select(attr =>
                !string.IsNullOrWhiteSpace(attr.Prefix)
                    ? attr.Prefix + attr.Delimiter + name
                    : name)
            .FirstOrDefault();

        return qattrib ?? name;
    }
}
