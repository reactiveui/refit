// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Http;

namespace Refit;

/// <summary>Base attribute describing the HTTP method used to send a request.</summary>
/// <seealso cref="System.Attribute" />
/// <remarks>
/// Initializes a new instance of the <see cref="HttpMethodAttribute"/> class.
/// </remarks>
/// <param name="path">The path.</param>
public abstract class HttpMethodAttribute(string path) : Attribute
{
    /// <summary>Gets the method.</summary>
    /// <value>
    /// The method.
    /// </value>
    public abstract HttpMethod Method { get; }

    /// <summary>Gets the path.</summary>
    /// <value>
    /// The path.
    /// </value>
    public virtual string Path { get; protected set; } = path;
}
