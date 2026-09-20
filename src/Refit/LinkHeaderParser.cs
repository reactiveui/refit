// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Reads the targets of a <c>Link</c> response header, as defined by RFC 8288.</summary>
internal static class LinkHeaderParser
{
    /// <summary>The separators between relation types in a <c>rel</c> parameter.</summary>
    private static readonly char[] RelationSeparators = [' ', '\t'];

    /// <summary>Finds the target of the first link that carries a relation.</summary>
    /// <param name="headerValues">The values of every <c>Link</c> header line; a line may hold several comma-separated links.</param>
    /// <param name="relation">The relation type to look for, such as <c>next</c>; compared without regard to case.</param>
    /// <returns>The link target, which may be relative, or <see langword="null"/> when no link carries the relation.</returns>
    internal static Uri? Find(IEnumerable<string> headerValues, string relation)
    {
        foreach (var value in headerValues)
        {
            var link = FindInValue(value, relation);
            if (link is not null)
            {
                return link;
            }
        }

        return null;
    }

    /// <summary>Finds the target of the first link in one header line that carries a relation.</summary>
    /// <param name="value">The header line.</param>
    /// <param name="relation">The relation type to look for.</param>
    /// <returns>The link target, or <see langword="null"/> when no link in the line carries the relation.</returns>
    private static Uri? FindInValue(string value, string relation)
    {
        var position = 0;
        while (TryReadTarget(value, ref position, out var target))
        {
            var hasRelation = ReadParameters(value, ref position, relation);
            if (hasRelation && Uri.TryCreate(target, UriKind.RelativeOrAbsolute, out var link))
            {
                return link;
            }
        }

        return null;
    }

    /// <summary>Reads the next <c>&lt;target&gt;</c> from a header line.</summary>
    /// <param name="value">The header line.</param>
    /// <param name="position">The read position, moved past the target.</param>
    /// <param name="target">The text between the angle brackets.</param>
    /// <returns><see langword="true"/> when a target was read; <see langword="false"/> at the end of the line or when it is malformed.</returns>
    private static bool TryReadTarget(string value, ref int position, out string target)
    {
        target = string.Empty;
        while (position < value.Length && (value[position] == ',' || char.IsWhiteSpace(value[position])))
        {
            position++;
        }

        if (position >= value.Length || value[position] != '<')
        {
            return false;
        }

        var end = value.IndexOf('>', position + 1);
        if (end < 0)
        {
            return false;
        }

        target = value.Substring(position + 1, end - position - 1);
        position = end + 1;
        return true;
    }

    /// <summary>Reads the parameters that follow a target, up to the comma that starts the next link.</summary>
    /// <param name="value">The header line.</param>
    /// <param name="position">The read position, moved past the parameters.</param>
    /// <param name="relation">The relation type to look for.</param>
    /// <returns><see langword="true"/> when a <c>rel</c> parameter names the relation.</returns>
    private static bool ReadParameters(string value, ref int position, string relation)
    {
        var hasRelation = false;
        while (position < value.Length)
        {
            var current = value[position];
            position++;
            if (current == ',')
            {
                break;
            }

            if (current == ';' && ReadParameter(value, ref position, relation))
            {
                hasRelation = true;
            }
        }

        return hasRelation;
    }

    /// <summary>Reads one <c>name=value</c> parameter.</summary>
    /// <param name="value">The header line.</param>
    /// <param name="position">The read position, moved past the parameter.</param>
    /// <param name="relation">The relation type to look for.</param>
    /// <returns><see langword="true"/> when the parameter is <c>rel</c> and names the relation.</returns>
    private static bool ReadParameter(string value, ref int position, string relation)
    {
        var nameStart = position;
        while (position < value.Length && value[position] is not ('=' or ';' or ','))
        {
            position++;
        }

        if (position >= value.Length || value[position] != '=')
        {
            return false;
        }

        var name = value.Substring(nameStart, position - nameStart).Trim();
        position++;
        var parameterValue = ReadParameterValue(value, ref position);
        return string.Equals(name, "rel", StringComparison.OrdinalIgnoreCase) && NamesRelation(parameterValue, relation);
    }

    /// <summary>Reads a parameter value, which is either a quoted string or a token.</summary>
    /// <param name="value">The header line.</param>
    /// <param name="position">The read position, moved past the value.</param>
    /// <returns>The unquoted value.</returns>
    private static string ReadParameterValue(string value, ref int position) =>
        position < value.Length && value[position] == '"'
            ? ReadQuotedString(value, ref position)
            : ReadToken(value, ref position);

    /// <summary>Reads a quoted string, resolving backslash escapes.</summary>
    /// <param name="value">The header line.</param>
    /// <param name="position">The read position, at the opening quote and moved past the closing one.</param>
    /// <returns>The text between the quotes.</returns>
    private static string ReadQuotedString(string value, ref int position)
    {
        var text = new System.Text.StringBuilder();
        position++;
        while (position < value.Length && value[position] != '"')
        {
            if (value[position] == '\\' && position + 1 < value.Length)
            {
                position++;
            }

            _ = text.Append(value[position]);
            position++;
        }

        position++;
        return text.ToString();
    }

    /// <summary>Reads an unquoted token, which ends at a separator or white space.</summary>
    /// <param name="value">The header line.</param>
    /// <param name="position">The read position, moved past the token.</param>
    /// <returns>The token.</returns>
    private static string ReadToken(string value, ref int position)
    {
        var start = position;
        while (position < value.Length && value[position] is not (';' or ',') && !char.IsWhiteSpace(value[position]))
        {
            position++;
        }

        return value.Substring(start, position - start);
    }

    /// <summary>Determines whether a <c>rel</c> value, which may list several relation types, names one.</summary>
    /// <param name="relationTypes">The space-separated relation types.</param>
    /// <param name="relation">The relation type to look for.</param>
    /// <returns><see langword="true"/> when one of the relation types equals <paramref name="relation"/>, without regard to case.</returns>
    private static bool NamesRelation(string relationTypes, string relation)
    {
        foreach (var relationType in relationTypes.Split(RelationSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(relationType, relation, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
