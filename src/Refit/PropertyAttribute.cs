// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// Used to store the value in HttpRequestMessage.Properties for further processing in a custom DelegatingHandler.
/// If a string is supplied to the constructor then it will be used as the key in the HttpRequestMessage.Properties dictionary.
/// If no key is specified then the key will be defaulted to the name of the parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class PropertyAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="PropertyAttribute"/> class.</summary>
    public PropertyAttribute()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="PropertyAttribute"/> class.</summary>
    /// <param name="key">The key.</param>
    public PropertyAttribute(string key) => Key = key;

    /// <summary>Gets the key under which to store the value on the HttpRequestMessage.Properties dictionary.</summary>
    public string? Key { get; }
}
