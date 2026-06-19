// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Override the key that will be sent in the query string.</summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AliasAsAttribute"/> class.
/// </remarks>
/// <param name="name">The name.</param>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class AliasAsAttribute(string name) : Attribute
{
    /// <summary>Gets the name.</summary>
    /// <value>
    /// The name.
    /// </value>
    public string Name { get; } = name;
}
