#if NET8_0_OR_GREATER
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Refit;

internal static class UriExt
{
    private const int StackallocThreshold = 512;
    private static readonly SearchValues<char> Unreserved =
        SearchValues.Create("-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz~");

    public static string EscapeDataString(string stringToEscape) =>
        EscapeString(stringToEscape, checkExistingEscaped: false, Unreserved);

    public static string EscapeString(string stringToEscape, bool checkExistingEscaped, SearchValues<char> noEscape)
    {
        ArgumentNullException.ThrowIfNull(stringToEscape);

        return EscapeString(stringToEscape, checkExistingEscaped, noEscape, stringToEscape);
    }

    public static string EscapeString(ReadOnlySpan<char> charsToEscape, bool checkExistingEscaped, SearchValues<char> noEscape, string? backingString)
    {
        Debug.Assert(!noEscape.Contains('%'), "Need to treat % specially; it should be part of any escaped set");
        Debug.Assert(backingString is null || backingString.Length == charsToEscape.Length);

        int indexOfFirstToEscape = charsToEscape.IndexOfAnyExcept(noEscape);
        if (indexOfFirstToEscape < 0)
        {
            // Nothing to escape, just return the original value.
            return backingString ?? charsToEscape.ToString();
        }

        // Otherwise, create a ValueStringBuilder to store the escaped data into,
        // escape the rest, and concat the result with the characters we skipped above.
        var vsb = new ValueStringBuilder(stackalloc char[StackallocThreshold]);

        // We may throw for very large inputs (when growing the ValueStringBuilder).
        vsb.EnsureCapacity(charsToEscape.Length);

        EscapeStringToBuilder(charsToEscape.Slice(indexOfFirstToEscape), ref vsb, noEscape, checkExistingEscaped);

        string result = string.Concat(charsToEscape.Slice(0, indexOfFirstToEscape), vsb.AsSpan());
        vsb.Dispose();
        return result;
    }


    private static void EscapeStringToBuilder(
        scoped ReadOnlySpan<char> stringToEscape, ref ValueStringBuilder vsb,
        SearchValues<char> noEscape, bool checkExistingEscaped)
    {
        Debug.Assert(!stringToEscape.IsEmpty && !noEscape.Contains(stringToEscape[0]));

        // Allocate enough stack space to hold any Rune's UTF8 encoding.
        Span<byte> utf8Bytes = stackalloc byte[4];

        while (!stringToEscape.IsEmpty)
        {
            char c = stringToEscape[0];

            if (!char.IsAscii(c))
            {
                if (Rune.DecodeFromUtf16(stringToEscape, out Rune r, out int charsConsumed) != OperationStatus.Done)
                {
                    r = Rune.ReplacementChar;
                }

                Debug.Assert(stringToEscape.EnumerateRunes() is { } e && e.MoveNext() && e.Current == r);
                Debug.Assert(charsConsumed is 1 or 2);

                stringToEscape = stringToEscape.Slice(charsConsumed);

                // The rune is non-ASCII, so encode it as UTF8, and escape each UTF8 byte.
                r.TryEncodeToUtf8(utf8Bytes, out int bytesWritten);
                foreach (byte b in utf8Bytes.Slice(0, bytesWritten))
                {
                    PercentEncodeByte(b, ref vsb);
                }

                continue;
            }

            if (!noEscape.Contains(c))
            {
                // If we're checking for existing escape sequences, then if this is the beginning of
                // one, check the next two characters in the sequence.
                if (c == '%' && checkExistingEscaped)
                {
                    // If the next two characters are valid escaped ASCII, then just output them as-is.
                    if (stringToEscape.Length > 2 && char.IsAsciiHexDigit(stringToEscape[1]) && char.IsAsciiHexDigit(stringToEscape[2]))
                    {
                        vsb.Append('%');
                        vsb.Append(stringToEscape[1]);
                        vsb.Append(stringToEscape[2]);
                        stringToEscape = stringToEscape.Slice(3);
                        continue;
                    }
                }

                PercentEncodeByte((byte)c, ref vsb);
                stringToEscape = stringToEscape.Slice(1);
                continue;
            }

            // We have a character we don't want to escape. It's likely there are more, do a vectorized search.
            int charsToCopy = stringToEscape.IndexOfAnyExcept(noEscape);
            if (charsToCopy < 0)
            {
                charsToCopy = stringToEscape.Length;
            }
            Debug.Assert(charsToCopy > 0);

            vsb.Append(stringToEscape.Slice(0, charsToCopy));
            stringToEscape = stringToEscape.Slice(charsToCopy);
        }
    }

    private static void PercentEncodeByte(byte b, ref ValueStringBuilder to)
    {
        to.Append('%');
        HexConverter.ToCharsBuffer(b, to.AppendSpan(2), 0);
    }
}

internal static class HexConverter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToCharsBuffer(byte value, Span<char> buffer, int startingIndex = 0)
    {
        uint difference = (((uint)value & 0xF0U) << 4) + ((uint)value & 0x0FU) - 0x8989U;
        uint packedResult = ((((uint)(-(int)difference) & 0x7070U) >> 4) + difference + 0xB9B9U) | 0;

        buffer[startingIndex + 1] = (char)(packedResult & 0xFF);
        buffer[startingIndex] = (char)(packedResult >> 8);
    }
}
#endif
