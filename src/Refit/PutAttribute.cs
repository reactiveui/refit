// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Http;

namespace Refit;

/// <summary>Send the request with HTTP method 'PUT'.</summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PutAttribute"/> class.
/// </remarks>
/// <param name="path">The path.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PutAttribute(string path) : HttpMethodAttribute(path)
{
    /// <summary>Gets the method.</summary>
    /// <value>
    /// The method.
    /// </value>
    public override HttpMethod Method => HttpMethod.Put;
}
