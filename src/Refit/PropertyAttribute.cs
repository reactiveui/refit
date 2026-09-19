// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// Stores local context on HttpRequestMessage.Options on modern .NET, or Properties on older targets,
/// for further processing in a message handler. An explicit key overrides the parameter or interface property name.
/// </summary>
[System.Diagnostics.DebuggerDisplay("Property: {Key}")]
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class PropertyAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="PropertyAttribute"/> class.</summary>
    public PropertyAttribute()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="PropertyAttribute"/> class.</summary>
    /// <param name="key">The key.</param>
    public PropertyAttribute(string key) => Key = key;

    /// <summary>Gets the request Options/Properties key, or null to use the parameter or interface property name.</summary>
    public string? Key { get; }
}
