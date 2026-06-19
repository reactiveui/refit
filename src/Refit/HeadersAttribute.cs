// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Add multiple headers to the request.</summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HeadersAttribute"/> class.
/// </remarks>
/// <param name="headers">The headers.</param>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public sealed class HeadersAttribute(params string[] headers) : Attribute
{
    /// <summary>Gets the headers.</summary>
    /// <value>
    /// The headers.
    /// </value>
    public string[] Headers { get; } = headers ?? [];
}
