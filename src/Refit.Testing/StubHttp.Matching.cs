// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Refit.Testing;

/// <summary>Route, path, query, header and body matching helpers for <see cref="StubHttp"/>.</summary>
public sealed partial class StubHttp
{
    /// <summary>Matches the request path against a template, treating <c>{name}</c> segments as wildcards.</summary>
    /// <param name="template">The expected template (<c>"*"</c> matches any; relative or absolute).</param>
    /// <param name="actual">The request URI.</param>
    /// <returns><see langword="true"/> when the path matches the template.</returns>
    private static bool MatchesTemplate(string template, Uri? actual)
    {
        if (template == "*")
        {
            return true;
        }

        if (actual is null)
        {
            return false;
        }

        var expectedPath = template.Split('?')[0];

        // A relative template (e.g. "/users") matches the request's absolute path; an absolute template
        // matches the full scheme/host/path.
#if NETFRAMEWORK
        var isRelative = expectedPath.StartsWith("/", StringComparison.Ordinal);
#else
        var isRelative = expectedPath.StartsWith('/');
#endif
        var actualPath = isRelative ? actual.AbsolutePath : actual.GetLeftPart(UriPartial.Path);
        return SegmentsMatch(expectedPath, actualPath);
    }

    /// <summary>Compares two paths segment by segment, where an expected <c>{name}</c> segment matches any one segment.</summary>
    /// <param name="expected">The expected template path.</param>
    /// <param name="actual">The actual request path.</param>
    /// <returns><see langword="true"/> when the segments match.</returns>
    private static bool SegmentsMatch(string expected, string actual)
    {
        var expectedSegments = expected.Split('/');
        var actualSegments = actual.Split('/');
        if (expectedSegments.Length != actualSegments.Length)
        {
            return false;
        }

        for (var i = 0; i < expectedSegments.Length; i++)
        {
            var expectedSegment = expectedSegments[i];
            if (IsPlaceholder(expectedSegment))
            {
                if (actualSegments[i].Length == 0)
                {
                    return false;
                }

                continue;
            }

            if (!string.Equals(expectedSegment, actualSegments[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether a template segment is a <c>{name}</c> placeholder.</summary>
    /// <param name="segment">The template segment.</param>
    /// <returns><see langword="true"/> when the segment is a placeholder.</returns>
    private static bool IsPlaceholder(string segment) =>
        segment.Length >= 2 && segment[0] == '{' && segment[^1] == '}';

    /// <summary>Classifies a route into its priority tier.</summary>
    /// <param name="route">The route to classify.</param>
    /// <returns>The route's priority tier.</returns>
    private static RouteTier TierOf(RouteMatcher route) =>
        route switch
        {
            { Fallback: true } => RouteTier.Fallback,
            { Reusable: true } => RouteTier.Reusable,
            _ => RouteTier.OneShot
        };

    /// <summary>Applies the partial and exact query matchers, if any, to the request URI.</summary>
    /// <param name="route">The candidate route.</param>
    /// <param name="actual">The request URI.</param>
    /// <returns><see langword="true"/> when the query matches (or no query matcher is set).</returns>
    private static bool MatchesQuery(RouteMatcher route, Uri? actual)
    {
        var raw = actual?.Query.TrimStart('?') ?? string.Empty;
        var pairs = ParsePairs(raw);
        return (route.ExactQuery is null || ExactMatch(pairs, ParsePairs(route.ExactQuery)))
            && (route.ExactQueryParams is null || ExactMatch(pairs, route.ExactQueryParams))
            && (route.Query is null || ContainsAll(pairs, route.Query));
    }

    /// <summary>Reads and compares the request body for the exact-body and form-data matchers.</summary>
    /// <param name="route">The candidate route.</param>
    /// <param name="request">The incoming request.</param>
    /// <param name="cancellationToken">A token to cancel the body read.</param>
    /// <returns><see langword="true"/> when the body matches (or no body matcher is set).</returns>
    private static async Task<bool> MatchesBodyAsync(RouteMatcher route, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (route.Body is null && route.FormData is null)
        {
            return true;
        }

        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return (route.Body is null || string.Equals(body, route.Body, StringComparison.Ordinal))
            && (route.FormData is null || ContainsAll(ParsePairs(body), route.FormData));
    }

    /// <summary>Confirms the request carries each expected header, checking request and content headers.</summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="headers">The expected header name/value pairs.</param>
    /// <returns><see langword="true"/> when every expected header is present with the expected value.</returns>
    private static bool MatchesHeaders(HttpRequestMessage request, (string Name, string Value)[] headers)
    {
        foreach (var (name, value) in headers)
        {
            if (!HeaderMatches(request, name, value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether a single named header is present with the expected value.</summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="name">The header name.</param>
    /// <param name="value">The expected header value.</param>
    /// <returns><see langword="true"/> when the header matches.</returns>
    private static bool HeaderMatches(HttpRequestMessage request, string name, string value)
    {
        var present = TryGetHeader(request.Headers, name, out var actual) ||
            (request.Content is not null && TryGetHeader(request.Content.Headers, name, out actual));
        return present && string.Equals(actual, value, StringComparison.Ordinal);
    }

    /// <summary>Gets the combined value of a named header, if present.</summary>
    /// <param name="headers">The header collection to search.</param>
    /// <param name="name">The header name.</param>
    /// <param name="value">The comma-joined header value when found.</param>
    /// <returns><see langword="true"/> when the header exists.</returns>
    private static bool TryGetHeader(HttpHeaders headers, string name, out string? value)
    {
        if (headers.TryGetValues(name, out var values))
        {
            value = string.Join(", ", values);
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Parses a URL-encoded <c>key=value&amp;...</c> string into decoded pairs.</summary>
    /// <param name="encoded">The encoded query or form body.</param>
    /// <returns>The decoded key/value pairs, in order.</returns>
    private static List<(string Key, string Value)> ParsePairs(string encoded)
    {
        var result = new List<(string, string)>();
        if (encoded.Length == 0)
        {
            return result;
        }

        foreach (var pair in encoded.Split('&'))
        {
            if (pair.Length == 0)
            {
                continue;
            }

            var eq = pair.IndexOf('=');
            var key = eq < 0 ? pair : pair[..eq];
            var value = eq < 0 ? string.Empty : pair[(eq + 1)..];
            result.Add((Decode(key), Decode(value)));
        }

        return result;
    }

    /// <summary>URL-decodes a single query or form token.</summary>
    /// <param name="value">The encoded token.</param>
    /// <returns>The decoded token.</returns>
    private static string Decode(string value) => Uri.UnescapeDataString(value.Replace('+', ' '));

    /// <summary>Determines whether every expected pair is present in the actual pairs.</summary>
    /// <param name="actual">The pairs parsed from the request.</param>
    /// <param name="expected">The pairs that must all be present.</param>
    /// <returns><see langword="true"/> when all expected pairs are found.</returns>
    private static bool ContainsAll(List<(string Key, string Value)> actual, (string Key, string Value)[] expected)
    {
        foreach (var (key, value) in expected)
        {
            if (!Contains(actual, key, value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether the actual pairs are exactly the expected pairs (same set, no extras).</summary>
    /// <param name="actual">The pairs parsed from the request.</param>
    /// <param name="expected">The complete expected pairs.</param>
    /// <returns><see langword="true"/> when the pair sets are equal, ignoring order.</returns>
    private static bool ExactMatch(List<(string Key, string Value)> actual, IReadOnlyList<(string Key, string Value)> expected)
    {
        if (actual.Count != expected.Count)
        {
            return false;
        }

        foreach (var (key, value) in expected)
        {
            if (!Contains(actual, key, value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether a specific key/value pair is present.</summary>
    /// <param name="pairs">The pairs to search.</param>
    /// <param name="key">The key to find.</param>
    /// <param name="value">The value to find.</param>
    /// <returns><see langword="true"/> when the pair exists.</returns>
    private static bool Contains(List<(string Key, string Value)> pairs, string key, string value)
    {
        foreach (var pair in pairs)
        {
            if (pair.Key == key && pair.Value == value)
            {
                return true;
            }
        }

        return false;
    }
}
