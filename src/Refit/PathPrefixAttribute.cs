// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Prepends a shared route prefix to every method's relative path on the interface it decorates.</summary>
/// <remarks>
/// The prefix is joined to each method's route template with exactly one <c>/</c> between them, so
/// <c>[PathPrefix("/api/v2")]</c> combined with <c>[Get("/users")]</c> requests <c>/api/v2/users</c>. A leading or
/// trailing slash on the prefix, and a leading slash on the route, are all tolerated without producing a double
/// slash; an empty or whitespace prefix is a no-op. Joining happens before the path is merged with
/// <see cref="HttpClient.BaseAddress"/>, so the prefix is part of the relative path rather than the
/// base address, and existing <c>{placeholder}</c> substitution and query strings are preserved.
/// <para>
/// The prefix that applies is the one declared on the interface the client is generated for - the <c>T</c> in
/// <c>RestService.For&lt;T&gt;</c> or <c>AddRefitClient&lt;T&gt;</c>. It applies to every method the client exposes,
/// including methods inherited from base interfaces. Prefixes declared on base interfaces are not concatenated with
/// the client interface's prefix; a base interface's prefix applies only when that base interface is itself the
/// client type.
/// </para>
/// </remarks>
/// <param name="prefix">The route prefix prepended to every method's relative path.</param>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class PathPrefixAttribute(string prefix) : Attribute
{
    /// <summary>Gets the route prefix prepended to every method's relative path.</summary>
    public string Prefix { get; } = prefix;
}
