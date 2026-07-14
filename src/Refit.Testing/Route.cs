// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Http;

namespace Refit.Testing;

/// <summary>
/// Factory for the common <see cref="RouteMatcher"/> shapes, one per HTTP method. Each takes a path
/// template that mirrors the route on the Refit interface, e.g. <c>Route.Get("/users/{id}")</c>.
/// </summary>
public static class Route
{
    /// <summary>Matches any method for the given path template.</summary>
    /// <param name="template">The path template (relative or absolute; <c>{name}</c> matches one segment).</param>
    /// <returns>A route that matches any method.</returns>
    public static RouteMatcher Any(string template) => new() { Template = template };

    /// <summary>Matches a <c>GET</c> request for the given path template.</summary>
    /// <param name="template">The path template (relative or absolute; <c>{name}</c> matches one segment).</param>
    /// <returns>A <c>GET</c> route.</returns>
    public static RouteMatcher Get(string template) => new() { Method = HttpMethod.Get, Template = template };

    /// <summary>Matches a <c>POST</c> request for the given path template.</summary>
    /// <param name="template">The path template (relative or absolute; <c>{name}</c> matches one segment).</param>
    /// <returns>A <c>POST</c> route.</returns>
    public static RouteMatcher Post(string template) => new() { Method = HttpMethod.Post, Template = template };

    /// <summary>Matches a <c>PUT</c> request for the given path template.</summary>
    /// <param name="template">The path template (relative or absolute; <c>{name}</c> matches one segment).</param>
    /// <returns>A <c>PUT</c> route.</returns>
    public static RouteMatcher Put(string template) => new() { Method = HttpMethod.Put, Template = template };

    /// <summary>Matches a <c>DELETE</c> request for the given path template.</summary>
    /// <param name="template">The path template (relative or absolute; <c>{name}</c> matches one segment).</param>
    /// <returns>A <c>DELETE</c> route.</returns>
    public static RouteMatcher Delete(string template) => new() { Method = HttpMethod.Delete, Template = template };

    /// <summary>Matches a <c>PATCH</c> request for the given path template.</summary>
    /// <param name="template">The path template (relative or absolute; <c>{name}</c> matches one segment).</param>
    /// <returns>A <c>PATCH</c> route.</returns>
    public static RouteMatcher Patch(string template) => new() { Method = new("PATCH"), Template = template };

    /// <summary>Matches a <c>HEAD</c> request for the given path template.</summary>
    /// <param name="template">The path template (relative or absolute; <c>{name}</c> matches one segment).</param>
    /// <returns>A <c>HEAD</c> route.</returns>
    public static RouteMatcher Head(string template) => new() { Method = HttpMethod.Head, Template = template };

    /// <summary>Matches a request with the given method for the given path template.</summary>
    /// <param name="method">The HTTP method to match.</param>
    /// <param name="template">The path template (relative or absolute; <c>{name}</c> matches one segment).</param>
    /// <returns>A route for the given method.</returns>
    public static RouteMatcher For(HttpMethod method, string template) => new() { Method = method, Template = template };

    /// <summary>Matches any request not matched by a more specific route, tried after all one-shot and reusable
    /// routes regardless of declaration order.</summary>
    /// <returns>A catch-all fallback route.</returns>
    public static RouteMatcher Fallback() => new() { Template = "*", Fallback = true };
}
