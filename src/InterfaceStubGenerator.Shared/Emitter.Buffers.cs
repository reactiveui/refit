// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Low-level primitives that assemble generated source into pre-sized character buffers.</summary>
internal static partial class Emitter
{
    /// <summary>The cached indentation strings for the levels the emitter uses in its hot paths.</summary>
    private static readonly string[] IndentCache = BuildIndentCache();

#if NETSTANDARD2_0
    /// <summary>Delegate used to fill a generated string buffer.</summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <param name="destination">The target character buffer.</param>
    /// <param name="state">The caller supplied state.</param>
    private delegate void GeneratedStringAction<in TState>(char[] destination, TState state);
#else
    /// <summary>Delegate used to fill a generated string buffer.</summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <param name="destination">The target character buffer.</param>
    /// <param name="state">The caller supplied state.</param>
    private delegate void GeneratedStringAction<in TState>(Span<char> destination, TState state);
#endif

    /// <summary>Builds a generated indentation string.</summary>
    /// <param name="level">The indentation level.</param>
    /// <returns>The generated indentation.</returns>
    /// <remarks>Indentation levels are compile-time constants, so the common levels are cached once and shared
    /// instead of allocating an identical fresh string at every per-method and per-parameter call site. Levels beyond
    /// the cache allocate a fresh string, a fallback reached only by nesting deeper than the shared test fixtures.</remarks>
    [ExcludeFromCodeCoverage]
    internal static string Indent(int level) =>
        (uint)level < (uint)IndentCache.Length
            ? IndentCache[level]
            : new string(' ', level * CharsPerIndentation);

    /// <summary>Precomputes the shared indentation strings for levels 0 through <see cref="MaxCachedIndentLevel"/>.</summary>
    /// <returns>The cached indentation strings, indexed by level.</returns>
    internal static string[] BuildIndentCache()
    {
        var cache = new string[MaxCachedIndentLevel + 1];
        for (var level = 0; level <= MaxCachedIndentLevel; level++)
        {
            cache[level] = new(' ', level * CharsPerIndentation);
        }

        return cache;
    }

#if NETSTANDARD2_0
    /// <summary>Writes a C# string literal or the <c>null</c> keyword into a generated string buffer.</summary>
    /// <param name="destination">The target character buffer.</param>
    /// <param name="value">The value to quote, or <see langword="null"/>.</param>
    /// <param name="position">The current write position.</param>
    private static void AppendLiteralOrNull(char[] destination, string? value, ref int position)
    {
        if (value is null)
        {
            AppendText(destination, NullLiteral, ref position);
            return;
        }

        destination[position] = '"';
        position++;
        foreach (var character in value)
        {
            var escape = EscapeSequence(character);
            if (escape is null)
            {
                destination[position] = character;
                position++;
            }
            else
            {
                AppendText(destination, escape, ref position);
            }
        }

        destination[position] = '"';
        position++;
    }

    /// <summary>Writes the decimal rendering of a non-negative 32-bit integer into a generated string buffer.</summary>
    /// <param name="destination">The target character buffer.</param>
    /// <param name="value">The non-negative value to render (callers only pass <c>CollectionFormat</c> values).</param>
    /// <param name="position">The current write position.</param>
    private static void AppendInt32(char[] destination, int value, ref int position)
    {
        var end = position + Int32Length(value);
        var write = end;
        do
        {
            --write;
            destination[write] = (char)('0' + (value % DecimalRadix));
            value /= DecimalRadix;
        }
        while (value > 0);

        position = end;
    }
#else
    /// <summary>Writes a C# string literal or the <c>null</c> keyword into a generated string buffer.</summary>
    /// <param name="destination">The target character span.</param>
    /// <param name="value">The value to quote, or <see langword="null"/>.</param>
    /// <param name="position">The current write position.</param>
    internal static void AppendLiteralOrNull(Span<char> destination, string? value, ref int position)
    {
        if (value is null)
        {
            AppendText(destination, NullLiteral, ref position);
            return;
        }

        destination[position] = '"';
        position++;
        foreach (var character in value)
        {
            var escape = EscapeSequence(character);
            if (escape is null)
            {
                destination[position] = character;
                position++;
            }
            else
            {
                AppendText(destination, escape, ref position);
            }
        }

        destination[position] = '"';
        position++;
    }

    /// <summary>Writes the decimal rendering of a non-negative 32-bit integer into a generated string buffer.</summary>
    /// <param name="destination">The target character span.</param>
    /// <param name="value">The non-negative value to render (callers only pass <c>CollectionFormat</c> values).</param>
    /// <param name="position">The current write position.</param>
    internal static void AppendInt32(Span<char> destination, int value, ref int position)
    {
        var end = position + Int32Length(value);
        var write = end;
        do
        {
            --write;
            destination[write] = (char)('0' + (value % DecimalRadix));
            value /= DecimalRadix;
        }
        while (value > 0);

        position = end;
    }
#endif

#if NETSTANDARD2_0
    /// <summary>Creates a generated string using a pre-sized buffer.</summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <param name="length">The string length.</param>
    /// <param name="state">The caller supplied state.</param>
    /// <param name="action">The buffer fill callback.</param>
    /// <returns>The generated string.</returns>
    private static string CreateGeneratedString<TState>(
        int length,
        TState state,
        GeneratedStringAction<TState> action)
    {
        var destination = new char[length];
        action(destination, state);
        return new(destination);
    }

    /// <summary>Appends text into a generated string buffer.</summary>
    /// <param name="destination">The target character buffer.</param>
    /// <param name="text">The text to append.</param>
    /// <param name="position">The current write position.</param>
    private static void AppendText(char[] destination, string text, ref int position)
    {
        text.CopyTo(0, destination, position, text.Length);
        position += text.Length;
    }
#else
    /// <summary>Creates a generated string using <see cref="string.Create{TState}(int, TState, System.Buffers.SpanAction{char, TState})"/>.</summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <param name="length">The string length.</param>
    /// <param name="state">The caller supplied state.</param>
    /// <param name="action">The buffer fill callback.</param>
    /// <returns>The generated string.</returns>
    internal static string CreateGeneratedString<TState>(
        int length,
        TState state,
        GeneratedStringAction<TState> action) =>
        string.Create(
            length,
            (State: state, Action: action),
            static (destination, context) => context.Action(destination, context.State));

    /// <summary>Appends text into a generated string buffer.</summary>
    /// <param name="destination">The target character buffer.</param>
    /// <param name="text">The text to append.</param>
    /// <param name="position">The current write position.</param>
    internal static void AppendText(Span<char> destination, string text, ref int position)
    {
        text.AsSpan().CopyTo(destination[position..]);
        position += text.Length;
    }
#endif
}
