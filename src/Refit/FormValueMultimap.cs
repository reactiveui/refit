// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>Transforms a form source from a .NET representation to the appropriate HTTP form encoded representation.</summary>
/// <remarks>Performs field renaming and value formatting as specified in <see cref="QueryAttribute"/>s and
/// <see cref="RefitSettings.FormUrlEncodedParameterFormatter"/>. A given key may appear multiple times with the
/// same or different values.</remarks>
internal sealed class FormValueMultimap : IEnumerable<KeyValuePair<string?, string?>>
{
    /// <summary>The default delimiter composing a nested field key from its parent key, matching the query-flattening default.</summary>
    internal const string DefaultNestingDelimiter = ".";

    /// <summary>The maximum object-graph depth flattened before deeper nested values are dropped, guarding against runaway recursion.</summary>
    private const int MaxNestingDepth = 32;

    /// <summary>Holds the collected form key/value entries.</summary>
    private readonly List<KeyValuePair<string?, string?>> _formEntries = [];

    /// <summary>The complex values currently being flattened on the recursion path, guarding against reference cycles.</summary>
    private List<object>? _ancestors;

    /// <summary>Initializes a new instance of the <see cref="FormValueMultimap"/> class from a source object.</summary>
    /// <param name="source">The source object or dictionary to convert into form entries.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    internal FormValueMultimap(object? source, RefitSettings settings)
        : this(source, source?.GetType(), settings)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FormValueMultimap"/> class from a source object.</summary>
    /// <param name="source">The source object or dictionary to convert into form entries.</param>
    /// <param name="sourceType">The type whose readable public properties are flattened, or <see langword="null"/> when there is no source.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    [SuppressMessage(
        "Correctness",
        "SST2403",
        Justification = "Every field is assigned before the registered flattener receives the map, and the class is sealed, so the flattener only sees a fully initialized instance.")]
    internal FormValueMultimap(object? source, Type? sourceType, RefitSettings settings)
    {
        ArgumentExceptionHelper.ThrowIfNull(settings);

        ContentSerializer = settings.ContentSerializer;
        UrlParameterKeyFormatter = settings.UrlParameterKeyFormatter;

        if (source is null)
        {
            return;
        }

        if (source is IDictionary dictionary)
        {
            AddDictionary(dictionary, settings, null, DefaultNestingDelimiter, 0);
            return;
        }

        ReflectionHooks.RequireFormObjectFlattener()
            .AddObject(this, source, sourceType!, settings, null, DefaultNestingDelimiter, 0);
    }

    /// <summary>Gets the content serializer used to resolve field names.</summary>
    internal IHttpContentSerializer ContentSerializer { get; }

    /// <summary>Gets the formatter applied to property names that are not explicitly aliased.</summary>
    internal IUrlParameterKeyFormatter UrlParameterKeyFormatter { get; }

    /// <summary>Gets a key for each entry. If multiple entries share the same key, the key is returned multiple times.</summary>
    internal IEnumerable<string?> Keys => GetKeys();

    /// <summary>Returns an enumerator over the form key/value entries.</summary>
    /// <returns>An enumerator over the entries.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<KeyValuePair<string?, string?>> GetEnumerator() => _formEntries.GetEnumerator();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Creates a form value map using the declared source type for property discovery.</summary>
    /// <typeparam name="TSource">The declared source type.</typeparam>
    /// <param name="source">The source object or dictionary to convert into form entries.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    /// <returns>The created form value map.</returns>
    internal static FormValueMultimap Create<TSource>(
        TSource source,
        RefitSettings settings) =>
        source is null or IDictionary
            ? new(source, settings)
            : new FormValueMultimap(source, typeof(TSource), settings);

    /// <summary>Creates a form value map from source-generated field descriptors, avoiding reflection.</summary>
    /// <typeparam name="TBody">The declared body type.</typeparam>
    /// <param name="body">The body instance to flatten into form entries.</param>
    /// <param name="fields">The compile-time field descriptors for the body type.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    /// <param name="nameStyle">The attribute family the configured content serializer reads names from.</param>
    /// <returns>The created form value map.</returns>
    internal static FormValueMultimap CreateFromFields<TBody>(
        TBody body,
        FormField<TBody>[] fields,
        RefitSettings settings,
        ContentSerializerNameStyle nameStyle)
    {
        var map = new FormValueMultimap(null, settings);
        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            var value = field.Getter(body);
            var fieldName = field.ResolveFieldName(map.UrlParameterKeyFormatter, nameStyle);

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
            AddCollection(fieldName, enumerable, value, format, explicitCollectionFormat ?? settings.CollectionFormat, settings);
            return;
        }

        // A nested complex object flattens its public properties under "field<delimiter>child".
        if (!TryEnterComplex(value, depth))
        {
            return;
        }

        ReflectionHooks.RequireFormObjectFlattener()
            .AddObject(this, value, value.GetType(), settings, fieldName, delimiter, depth + 1);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
}
