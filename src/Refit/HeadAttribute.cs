// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Http;

namespace Refit;

/// <summary>Send the request with HTTP method 'HEAD'.</summary>
/// <param name="path">The path.</param>
/// <remarks>
/// Initializes a new instance of the <see cref="HeadAttribute"/> class.
/// </remarks>
[System.Diagnostics.DebuggerDisplay("{Method}")]
[AttributeUsage(AttributeTargets.Method)]
public sealed class HeadAttribute(string path) : HttpMethodAttribute(path)
{
    /// <summary>Gets the method.</summary>
    /// <value>
    /// The method.
    /// </value>
    public override HttpMethod Method => HttpMethod.Head;
}
