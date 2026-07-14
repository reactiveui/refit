// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading.Tasks;

namespace Refit.Testing;

/// <summary>
/// Matches an incoming request by HTTP method and a path <see cref="Template"/> that mirrors the
/// <c>[Get("/users/{id}")]</c> attributes on a Refit interface. Build the common cases with the
/// <see cref="Route"/> factory; set the optional properties directly for finer matching.
/// </summary>
/// <remarks>
/// The template is matched a segment at a time: a <c>{name}</c> segment matches any single non-empty
/// path segment, and every other segment must match literally. A relative template (<c>"/users/1"</c>)
/// matches the request's absolute path; an absolute template (<c>"https://api/users/1"</c>) matches the
/// whole scheme/host/path. Use <c>"*"</c> to match any path. The query string is matched by
/// <see cref="Query"/>, not the template.
/// </remarks>
public sealed class RouteMatcher
{
    /// <summary>Gets the HTTP method to match; <see langword="null"/> matches any method.</summary>
    public HttpMethod? Method { get; init; }

    /// <summary>Gets the path template to match (relative or absolute; <c>{name}</c> matches one segment; <c>"*"</c> matches any path).</summary>
    public required string Template { get; init; }

    /// <summary>Gets query key/value pairs the request must contain (a partial match; other params allowed).</summary>
    public (string Key, string Value)[]? Query { get; init; }

    /// <summary>Gets the exact raw query string (without the leading <c>?</c>) the request must have.</summary>
    public string? ExactQuery { get; init; }

    /// <summary>Gets the complete, decoded set of query pairs the request must have (no extras, order-insensitive).</summary>
    public (string Key, string Value)[]? ExactQueryParams { get; init; }

    /// <summary>Gets header name/value pairs the request must contain (request or content headers).</summary>
    public (string Name, string Value)[]? Headers { get; init; }

    /// <summary>Gets the exact request body the request must carry.</summary>
    public string? Body { get; init; }

    /// <summary>Gets form-encoded key/value pairs the request body must contain (a partial match).</summary>
    public (string Key, string Value)[]? FormData { get; init; }

    /// <summary>Gets an arbitrary predicate the request must satisfy.</summary>
    public Func<HttpRequestMessage, bool>? Where { get; init; }

    /// <summary>Gets an asynchronous predicate the request must satisfy, for matchers that await (e.g. reading the request body).</summary>
    public Func<HttpRequestMessage, Task<bool>>? WhereAsync { get; init; }

    /// <summary>Gets a value indicating whether this route may match repeatedly and is not required by <see cref="StubHttp.VerifyAllCalled"/>.</summary>
    public bool Reusable { get; init; }

    /// <summary>
    /// Gets a value indicating whether this route is a catch-all fallback, tried only after every one-shot and
    /// reusable route fails to match, regardless of where it appears in the table. Like a reusable route it may match
    /// any number of requests and is not required by <see cref="StubHttp.VerifyAllCalled"/>. Build one with
    /// <see cref="Route.Fallback()"/>.
    /// </summary>
    public bool Fallback { get; init; }
}
