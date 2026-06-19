// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Controls how query parameters are encoded into the request Uri.</summary>
/// <seealso cref="System.Attribute" />
/// <remarks>
/// Initializes a new instance of the <see cref="QueryUriFormatAttribute"/> class.
/// </remarks>
/// <param name="uriFormat">The URI format.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class QueryUriFormatAttribute(UriFormat uriFormat) : Attribute
{
    /// <summary>Gets how the Query Params should be encoded.</summary>
    public UriFormat UriFormat { get; } = uriFormat;
}
