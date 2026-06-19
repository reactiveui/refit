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
            if (value < 0)
            {
                Throw();
                static void Throw() => throw new ArgumentOutOfRangeException(nameof(value));
            }

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
        var remainingText = text.AsSpan();
        do
        {
            var nextLine = GetNextLine(ref remainingText, out isFinalLine);

            if (!nextLine.IsEmpty)
            {
                AddIndentation();
            }

            AppendSpan(_sb, nextLine);
            _sb.AppendLine();
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

    /// <summary>Extracts the next line from the remaining text.</summary>
    /// <param name="remainingText">The remaining text, advanced past the returned line.</param>
    /// <param name="isFinalLine">Set to <c>true</c> when the returned line is the last one.</param>
    /// <returns>The next line of text.</returns>
    private static ReadOnlySpan<char> GetNextLine(
        ref ReadOnlySpan<char> remainingText,
        out bool isFinalLine)
    {
        if (remainingText.IsEmpty)
        {
            isFinalLine = true;
            return default;
        }

        ReadOnlySpan<char> rest;

        var lineLength = remainingText.IndexOf('\n');
        if (lineLength == -1)
        {
            lineLength = remainingText.Length;
            isFinalLine = true;
            rest = default;
        }
        else
        {
            rest = remainingText[(lineLength + 1)..];
            isFinalLine = false;
        }

        if ((uint)lineLength > 0 && remainingText[lineLength - 1] == '\r')
        {
            lineLength--;
        }

        var next = remainingText[..lineLength];
        remainingText = rest;
        return next;
    }

    /// <summary>Appends a span of characters to the given builder.</summary>
    /// <param name="builder">The builder to append to.</param>
    /// <param name="span">The characters to append.</param>
    private static unsafe void AppendSpan(StringBuilder builder, ReadOnlySpan<char> span)
    {
        fixed (char* ptr = span)
        {
            builder.Append(ptr, span.Length);
        }
    }

    /// <summary>Appends the indentation characters for the current indentation level.</summary>
    private void AddIndentation() =>
        _sb.Append(IndentationChar, CharsPerIndentation * _indentation);
}
