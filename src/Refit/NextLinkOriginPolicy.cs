// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// Decides which server-supplied next-page links a paging helper may follow. A followed link is sent through the same
/// client as every other request, so it carries that client's credentials to whatever origin the link names.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public sealed class NextLinkOriginPolicy
{
    /// <summary>The allowed origins, or <see langword="null"/> when any http or https origin is allowed.</summary>
    private readonly Uri[]? _origins;

    /// <summary>Initializes a new instance of the <see cref="NextLinkOriginPolicy"/> class.</summary>
    /// <param name="origins">The allowed origins, or <see langword="null"/> to allow any http or https origin.</param>
    private NextLinkOriginPolicy(Uri[]? origins) => _origins = origins;

    /// <summary>Gets a policy that follows a link to any http or https origin, forwarding the client's credentials to it.</summary>
    public static NextLinkOriginPolicy Unrestricted { get; } = new(null);

    /// <summary>Creates a policy that follows only links whose scheme, host and port match one of the given origins.</summary>
    /// <param name="origins">The allowed origins; only the scheme, host and port of each are used. Relative links resolve against the first one when no request URI is available.</param>
    /// <returns>A policy restricted to <paramref name="origins"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="origins"/> or one of its elements is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="origins"/> is empty, or an element is not an absolute http or https URI.</exception>
    public static NextLinkOriginPolicy Allow(params Uri[] origins)
    {
        ArgumentExceptionHelper.ThrowIfNull(origins);
        if (origins.Length == 0)
        {
            throw new ArgumentException("At least one origin is required.", nameof(origins));
        }

        var copy = new Uri[origins.Length];
        for (var i = 0; i < origins.Length; i++)
        {
            var origin = origins[i] ?? throw new ArgumentNullException(nameof(origins), "An origin is null.");
            if (!origin.IsAbsoluteUri || !IsHttp(origin))
            {
                throw new ArgumentException("Every origin must be an absolute http or https URI.", nameof(origins));
            }

            copy[i] = origin;
        }

        return new(copy);
    }

    /// <summary>Gets a value indicating whether a paging helper may follow <paramref name="link"/>.</summary>
    /// <param name="link">The link to test.</param>
    /// <returns><see langword="true"/> for an absolute http or https link without user information on an allowed origin.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="link"/> is <see langword="null"/>.</exception>
    public bool IsAllowed(Uri link)
    {
        ArgumentExceptionHelper.ThrowIfNull(link);

        if (!link.IsAbsoluteUri || !IsHttp(link) || link.UserInfo.Length != 0)
        {
            return false;
        }

        if (_origins is null)
        {
            return true;
        }

        foreach (var origin in _origins)
        {
            if (string.Equals(origin.Scheme, link.Scheme, StringComparison.Ordinal)
                && origin.Port == link.Port
                && string.Equals(origin.IdnHost, link.IdnHost, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (_origins is null)
        {
            return "Unrestricted";
        }

        var origins = new string[_origins.Length];
        for (var i = 0; i < origins.Length; i++)
        {
            origins[i] = Describe(_origins[i]);
        }

        return $"Allow({string.Join(", ", origins)})";
    }

    /// <summary>Resolves a possibly relative link and returns it only when it is allowed.</summary>
    /// <param name="link">The link taken from a page.</param>
    /// <param name="referrer">The absolute URI the link was found at, or <see langword="null"/> when unknown.</param>
    /// <returns>The absolute link.</returns>
    /// <exception cref="InvalidOperationException">The link cannot be resolved to an absolute URI, or names a refused origin.</exception>
    internal Uri Resolve(Uri link, Uri? referrer)
    {
        var absolute = link;
        if (!link.IsAbsoluteUri)
        {
            var baseUri = referrer ?? (_origins is null ? null : _origins[0]);
            absolute = baseUri is null
                ? throw new InvalidOperationException("A relative next link cannot be resolved because no request URI or allowed origin is available.")
                : new Uri(baseUri, link);
        }

        return IsAllowed(absolute)
            ? absolute
            : throw new InvalidOperationException(
                $"The next link points at '{Describe(absolute)}', which the origin policy does not allow, so it was not requested.");
    }

    /// <summary>Gets a value indicating whether the URI uses the http or https scheme.</summary>
    /// <param name="uri">An absolute URI.</param>
    /// <returns><see langword="true"/> when the scheme is http or https.</returns>
    private static bool IsHttp(Uri uri) =>
        string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
        || string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal);

    /// <summary>Describes a URI by its origin only, so query strings that may hold tokens never reach a message.</summary>
    /// <param name="uri">An absolute URI.</param>
    /// <returns>The scheme, host and port, without user information.</returns>
    private static string Describe(Uri uri) => $"{uri.Scheme}://{uri.Authority}";
}
