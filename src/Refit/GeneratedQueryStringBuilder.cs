// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Refit;

/// <summary>
/// Appends query parameters to a relative request path without reflection, matching the escaping, ordering,
/// null-omission and collection-format semantics of the reflection request builder. Used by source-generated
/// request construction; the API is also callable directly by hand-written AOT-friendly clients.
/// </summary>
/// <remarks>
/// Values must already be formatted (see <see cref="GeneratedRequestRunner.FormatInvariant{T}(T, string?)"/> and
/// <see cref="IUrlParameterFormatter"/>); a <see langword="null"/> formatted value omits its parameter. Query keys
/// and values are escaped with <see cref="Uri.EscapeDataString(string)"/> unless a call passes
/// <c>preEncoded: true</c> (the <see cref="EncodedAttribute"/> contract).
/// </remarks>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public ref struct GeneratedQueryStringBuilder
{
    /// <summary>The extra capacity reserved beyond the path when the first parameter is appended.</summary>
    private const int InitialQueryCapacity = 128;

#if NET6_0_OR_GREATER
    /// <summary>The stack buffer size for span-formatting a single query value; larger renderings grow a rented buffer.</summary>
    private const int FormatBufferLength = 128;

    /// <summary>The factor by which the rented span-formatting buffer grows when a value overflows the current buffer.</summary>
    private const int BufferGrowthFactor = 2;
#endif

    /// <summary>The relative path the query string is appended to.</summary>
    private readonly string _relativePath;

    /// <summary>The accumulating path plus query text; unused until the first parameter is appended.</summary>
    private ValueStringBuilder _text;

    /// <summary>The accumulating joined collection value while inside a non-multi collection.</summary>
    private ValueStringBuilder _joinedValues;

    /// <summary>The key of the collection currently being appended, or null outside a collection.</summary>
    private string? _collectionKey;

    /// <summary>The delimiter between joined collection values.</summary>
    private char _collectionDelimiter;

    /// <summary>Whether the current collection renders one <c>key=value</c> pair per element.</summary>
    private bool _collectionIsMulti;

    /// <summary>Whether the current collection is caller-encoded.</summary>
    private bool _collectionPreEncoded;

    /// <summary>The number of values appended to the current joined collection.</summary>
    private int _collectionValueCount;

    /// <summary>Whether the path or an appended parameter already established a query string.</summary>
    private bool _hasQuery;

    /// <summary>Whether any parameter has been appended, requiring <see cref="Build"/> to materialize new text.</summary>
    private bool _hasAppended;

    /// <summary>Initializes a new instance of the <see cref="GeneratedQueryStringBuilder"/> struct.</summary>
    /// <param name="relativePath">The relative path, whose template query string (if any) is preserved in front
    /// of appended parameters. Dynamic path segments must already be escaped.</param>
    public GeneratedQueryStringBuilder(string relativePath)
    {
        _relativePath = relativePath;
        _text = default;
        _joinedValues = default;
        _hasQuery = StringHelpers.Contains(relativePath, '?');
    }

    /// <summary>Appends one <c>key=value</c> query parameter.</summary>
    /// <param name="name">The query key.</param>
    /// <param name="value">The formatted value; the parameter is omitted when this is <see langword="null"/>.</param>
    /// <param name="preEncoded">Whether the key and value are caller-encoded and appended verbatim.</param>
    public void Add(string name, string? value, bool preEncoded)
    {
        if (value is null)
        {
            return;
        }

        AppendPair(name, value, preEncoded);
    }

#if NET6_0_OR_GREATER
    /// <summary>Appends one <c>key=value</c> query parameter, formatting an <see cref="ISpanFormattable"/> value straight
    /// into the query buffer with no intermediate formatted string.</summary>
    /// <typeparam name="T">The span-formattable value type.</typeparam>
    /// <param name="name">The query key.</param>
    /// <param name="value">The value to render; the generator routes only non-null values here.</param>
    /// <param name="format">The compile-time format from <c>[Query(Format = ...)]</c>, or null.</param>
    /// <param name="preEncoded">Whether the key and value are caller-encoded and appended verbatim.</param>
    /// <remarks>The value is rendered invariant and escaped to produce exactly what <see cref="Add"/> yields for the
    /// same rendered string. On targets without <c>Uri.EscapeDataString(ReadOnlySpan&lt;char&gt;)</c> the generator only
    /// routes a URL-unreserved integer here, whose formatted span (digits and an optional <c>-</c>) needs no escaping.</remarks>
    public void AddFormatted<T>(string name, T value, string? format, bool preEncoded)
        where T : ISpanFormattable =>
        AppendFormattedPair(name, value, format, preEncoded);
#endif

    /// <summary>Appends one valueless query flag (<c>?name</c>).</summary>
    /// <param name="name">The formatted flag name; the flag is omitted when this is <see langword="null"/>.</param>
    /// <param name="preEncoded">Whether the name is caller-encoded and appended verbatim.</param>
    public void AddFlag(string? name, bool preEncoded)
    {
        if (name is null)
        {
            return;
        }

        AppendSeparator();
        _text.Append(preEncoded ? name : StringHelpers.EscapeDataString(name));
    }

    /// <summary>Starts a collection-valued parameter fed by <see cref="AddCollectionValue(string?)"/> calls and finished by <see cref="EndCollection"/>.</summary>
    /// <param name="name">The query key.</param>
    /// <param name="collectionFormat">The resolved collection format (an explicit attribute value, or the
    /// <see cref="RefitSettings.CollectionFormat"/> default).</param>
    /// <param name="preEncoded">Whether the key and values are caller-encoded and appended verbatim.</param>
    public void BeginCollection(string name, CollectionFormat collectionFormat, bool preEncoded)
    {
        Debug.Assert(_collectionKey is null, "BeginCollection must not be nested.");
        _collectionKey = name;
        _collectionIsMulti = collectionFormat == CollectionFormat.Multi;
        _collectionPreEncoded = preEncoded;
        _collectionValueCount = 0;
        _collectionDelimiter = collectionFormat switch
        {
            CollectionFormat.Ssv => ' ',
            CollectionFormat.Tsv => '\t',
            CollectionFormat.Pipes => '|',
            _ => ','
        };
    }

    /// <summary>Appends one formatted element of the current collection.</summary>
    /// <param name="value">The formatted element value; under <see cref="CollectionFormat.Multi"/> a
    /// <see langword="null"/> element is omitted, otherwise it joins as an empty value.</param>
    public void AddCollectionValue(string? value)
    {
        Debug.Assert(_collectionKey is not null, "AddCollectionValue requires BeginCollection.");
        if (_collectionIsMulti)
        {
            if (value is not null)
            {
                AppendPair(_collectionKey!, value, _collectionPreEncoded);
            }

            return;
        }

        if (_collectionValueCount++ > 0)
        {
            _joinedValues.Append(_collectionDelimiter);
        }

        _joinedValues.Append(value);
    }

#if NET6_0_OR_GREATER
    /// <summary>Appends one <see cref="ISpanFormattable"/> element of the current collection, formatting it straight into
    /// the query buffer with no intermediate formatted string.</summary>
    /// <typeparam name="T">The span-formattable element type.</typeparam>
    /// <param name="value">The element to render; the generator routes only non-null values here.</param>
    /// <remarks>The element is rendered invariant with no format. Under <see cref="CollectionFormat.Multi"/> it becomes
    /// its own escaped <c>key=value</c> pair; otherwise it joins into the value that <see cref="EndCollection"/> escapes
    /// as a whole, so the joined element is appended unescaped exactly like <see cref="AddCollectionValue"/>.</remarks>
    public void AddCollectionValueFormatted<T>(T value)
        where T : ISpanFormattable
    {
        Debug.Assert(_collectionKey is not null, "AddCollectionValueFormatted requires BeginCollection.");
        if (_collectionIsMulti)
        {
            AppendFormattedPair(_collectionKey!, value, null, _collectionPreEncoded);
            return;
        }

        if (_collectionValueCount++ > 0)
        {
            _joinedValues.Append(_collectionDelimiter);
        }

        AppendFormattedValue(ref _joinedValues, value, null, escape: false);
    }
#endif

    /// <summary>Finishes the current collection, emitting the joined <c>key=value</c> pair for non-multi formats.</summary>
    public void EndCollection()
    {
        Debug.Assert(_collectionKey is not null, "EndCollection requires BeginCollection.");
        if (!_collectionIsMulti)
        {
            // A joined collection always emits its pair, even when the collection was empty (key=),
            // matching the reflection request builder.
            AppendPair(_collectionKey!, _joinedValues.ToString(), _collectionPreEncoded);
        }

        _collectionKey = null;
    }

    /// <summary>Builds the final relative path with the appended query string and releases pooled buffers.</summary>
    /// <returns>The relative path, unchanged when no parameter was appended.</returns>
    public string Build()
    {
        _joinedValues.Dispose();
        if (!_hasAppended)
        {
            _text.Dispose();
            return _relativePath;
        }

        return _text.ToString();
    }

    /// <summary>Appends the <c>?</c> or <c>&amp;</c> separator, materializing the text buffer on first use.</summary>
    private void AppendSeparator()
    {
        if (!_hasAppended)
        {
            _text.EnsureCapacity(_relativePath.Length + InitialQueryCapacity);
            _text.Append(_relativePath);
            _hasAppended = true;
        }

        _text.Append(_hasQuery ? '&' : '?');
        _hasQuery = true;
    }

    /// <summary>Appends one <c>key=value</c> pair with the configured escaping.</summary>
    /// <param name="name">The query key.</param>
    /// <param name="value">The non-null formatted value.</param>
    /// <param name="preEncoded">Whether the key and value are appended verbatim.</param>
    private void AppendPair(string name, string value, bool preEncoded)
    {
        AppendSeparator();
        if (preEncoded)
        {
            _text.Append(name);
            _text.Append('=');
            _text.Append(value);
            return;
        }

        _text.Append(StringHelpers.EscapeDataString(name));
        _text.Append('=');
        _text.Append(StringHelpers.EscapeDataString(value));
    }

#if NET6_0_OR_GREATER
    /// <summary>Appends one <c>key=value</c> pair, formatting a span-formattable value straight into the buffer.</summary>
    /// <typeparam name="T">The span-formattable value type.</typeparam>
    /// <param name="name">The query key.</param>
    /// <param name="value">The value to render.</param>
    /// <param name="format">The compile-time format, or null.</param>
    /// <param name="preEncoded">Whether the key and value are appended verbatim.</param>
    private void AppendFormattedPair<T>(string name, T value, string? format, bool preEncoded)
        where T : ISpanFormattable
    {
        AppendSeparator();
        _text.Append(preEncoded ? name : StringHelpers.EscapeDataString(name));
        _text.Append('=');
        AppendFormattedValue(ref _text, value, format, escape: !preEncoded);
    }

    /// <summary>Formats a span-formattable value into a stack buffer (growing a rented buffer when it overflows) and
    /// appends it to the target, escaping the formatted span in place when requested.</summary>
    /// <typeparam name="T">The span-formattable value type.</typeparam>
    /// <param name="target">The buffer receiving the rendered value.</param>
    /// <param name="value">The value to render.</param>
    /// <param name="format">The compile-time format, or null for the default rendering.</param>
    /// <param name="escape">Whether the formatted span is URI-data-escaped before it is appended.</param>
    private readonly void AppendFormattedValue<T>(ref ValueStringBuilder target, T value, string? format, bool escape)
        where T : ISpanFormattable
    {
        Span<char> buffer = stackalloc char[FormatBufferLength];
        char[]? rented = null;
        try
        {
            int written;
            while (!value.TryFormat(buffer, out written, format.AsSpan(), System.Globalization.CultureInfo.InvariantCulture))
            {
                if (rented is not null)
                {
                    System.Buffers.ArrayPool<char>.Shared.Return(rented);
                }

                rented = System.Buffers.ArrayPool<char>.Shared.Rent(buffer.Length * BufferGrowthFactor);
                buffer = rented;
            }

            var formatted = (ReadOnlySpan<char>)buffer[..written];
#if NET9_0_OR_GREATER
            if (escape)
            {
                target.Append(Uri.EscapeDataString(formatted));
                return;
            }
#else
            // Pre-net9 has no span overload of Uri.EscapeDataString; the generator only routes a URL-unreserved integer
            // to the escaping path, whose digits (and an optional leading '-') are already URL-safe, so it is appended
            // verbatim just like an unescaped value.
            _ = escape;
#endif

            // Copy into a reserved slice so the stack buffer is never captured by the builder (ref-safety), matching a
            // verbatim span append with no intermediate string.
            formatted.CopyTo(target.AppendSpan(written));
        }
        finally
        {
            if (rented is not null)
            {
                System.Buffers.ArrayPool<char>.Shared.Return(rented);
            }
        }
    }
#endif
}
