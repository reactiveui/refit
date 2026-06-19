// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Add the Authorize header to the request with the value of the associated parameter.</summary>
/// <remarks>
/// Default authorization scheme: Bearer.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class.
/// </remarks>
/// <param name="scheme">The scheme.</param>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class AuthorizeAttribute(string scheme = "Bearer") : Attribute
{
    /// <summary>Gets the scheme.</summary>
    /// <value>
    /// The scheme.
    /// </value>
    public string Scheme { get; } = scheme;
}
