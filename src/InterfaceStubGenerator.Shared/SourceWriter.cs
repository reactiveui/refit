// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Refit.Generator;

// From https://github.com/dotnet/runtime/blob/233826c88d2100263fb9e9535d96f75824ba0aea/src/libraries/Common/src/SourceGenerators/SourceWriter.cs#L11
/// <summary>A lightweight indentation-aware writer used to build generated source text.</summary>
internal sealed class SourceWriter
{
    /// <summary>The character used for a single unit of indentation.</summary>
    private const char IndentationChar = ' ';

    /// <summary>The number of indentation characters per indentation level.</summary>
    private const int CharsPerIndentation = 4;

    // Pre-size the buffer for a typical generated interface stub so the common case avoids
    // repeated StringBuilder doublings (a fresh writer is created per interface).
    /// <summary>The initial capacity used for the underlying buffer.</summary>
    private const int DefaultCapacity = 4096;

    /// <summary>The underlying buffer that accumulates the written text.</summary>
    private readonly StringBuilder _sb;

    /// <summary>The current indentation level.</summary>
    private int _indentation;

    /// <summary>Initializes a new instance of the <see cref="SourceWriter"/> class.</summary>
    public SourceWriter()
        : this(DefaultCapacity)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SourceWriter"/> class.</summary>
    /// <param name="capacity">The initial backing buffer capacity.</param>
    public SourceWriter(int capacity) => _sb = new(capacity);

    /// <summary>Gets or sets the current indentation level.</summary>
    public int Indentation
    {
        get => _indentation;
        set
        {
            ArgumentOutOfRangeExceptionHelper.ThrowIfNegative(value);
            _indentation = value;
        }
    }

    /// <summary>Appends the given text without any indentation or line break.</summary>
    /// <param name="text">The text to append.</param>
    public void Append(string text) => _sb.Append(text);

    /// <summary>Appends a single character without any indentation or line break.</summary>
    /// <param name="value">The character to append.</param>
    public void Append(char value) => _sb.Append(value);

    /// <summary>Appends indentation for the current indentation level.</summary>
    public void WriteIndentation() => AddIndentation();

    /// <summary>Writes the given text, applying the current indentation to each line.</summary>
    /// <param name="text">The text to write.</param>
    public void WriteLine(string text)
    {
        if (_indentation == 0)
        {
            _sb.AppendLine(text);
            return;
        }

        bool isFinalLine;
        var lineStart = 0;
        do
        {
            var lineLength = GetNextLineLength(
                text,
                lineStart,
                out var nextLineStart,
                out isFinalLine);

            if (lineLength > 0)
            {
                AddIndentation();
            }

            _sb.Append(text, lineStart, lineLength).AppendLine();
            lineStart = nextLineStart;
        } while (!isFinalLine);
    }

    /// <summary>Writes an empty line.</summary>
    public void WriteLine() => _sb.AppendLine();

    /// <summary>Produces a <see cref="SourceText"/> from the accumulated content.</summary>
    /// <returns>The accumulated content as a <see cref="SourceText"/>.</returns>
    public SourceText ToSourceText()
    {
        Debug.Assert(_indentation == 0 && _sb.Length > 0, "Source text should be produced only when indentation is reset and content has been written.");
        return SourceText.From(_sb.ToString(), Encoding.UTF8);
    }

    /// <summary>Clears the accumulated content and resets the indentation.</summary>
    public void Reset()
    {
        _sb.Clear();
        _indentation = 0;
    }

    /// <summary>Gets the length of the next line in the supplied text.</summary>
    /// <param name="text">The text to inspect.</param>
    /// <param name="lineStart">The start index of the next line.</param>
    /// <param name="nextLineStart">Set to the index where the following line starts.</param>
    /// <param name="isFinalLine">Set to <c>true</c> when the returned line is the last one.</param>
    /// <returns>The next line length, excluding a trailing carriage return before a newline.</returns>
    private static int GetNextLineLength(
        string text,
        int lineStart,
        out int nextLineStart,
        out bool isFinalLine)
    {
        if ((uint)lineStart >= (uint)text.Length)
        {
            nextLineStart = text.Length;
            isFinalLine = true;
            return 0;
        }

        var newLineIndex = text.IndexOf('\n', lineStart);
        int lineLength;
        if (newLineIndex == -1)
        {
            lineLength = text.Length - lineStart;
            nextLineStart = text.Length;
            isFinalLine = true;
        }
        else
        {
            lineLength = newLineIndex - lineStart;
            nextLineStart = newLineIndex + 1;
            isFinalLine = false;
        }

        if (lineLength > 0 && text[lineStart + lineLength - 1] == '\r')
        {
            lineLength--;
        }

        return lineLength;
    }

    /// <summary>Appends the indentation characters for the current indentation level.</summary>
    private void AddIndentation() =>
        _sb.Append(IndentationChar, CharsPerIndentation * _indentation);
}
