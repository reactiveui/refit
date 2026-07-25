// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Net.Http;

namespace Refit.Benchmarks;

/// <summary>A custom HTTP verb attribute (the draft-standard QUERY method) used to exercise the cached verb instance.</summary>
/// <remarks>Initializes a new instance of the <see cref="QueryVerbAttribute"/> class.</remarks>
/// <param name="path">The relative request path.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class QueryVerbAttribute(string path) : HttpMethodAttribute(path)
{
    /// <inheritdoc/>
    public override HttpMethod Method => new("QUERY");
}
