// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Add a header to the request.</summary>
/// <param name="header">The header.</param>
/// <remarks>
/// Initializes a new instance of the <see cref="HeaderAttribute"/> class.
/// </remarks>
[System.Diagnostics.DebuggerDisplay("{Header}")]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class HeaderAttribute(string header) : Attribute
{
    /// <summary>Gets the header.</summary>
    /// <value>
    /// The header.
    /// </value>
    public string Header { get; } = header;
}
