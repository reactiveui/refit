// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;

// This stand-in has the same full name as Newtonsoft.Json's attribute so the by-name lookup can be tested here
// without taking a Newtonsoft.Json reference in this test project.
namespace Newtonsoft.Json;

/// <summary>Stand-in for Newtonsoft.Json's property-name attribute.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class JsonPropertyAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="JsonPropertyAttribute"/> class.</summary>
    /// <param name="propertyName">The serialized property name.</param>
    public JsonPropertyAttribute(string propertyName) => PropertyName = propertyName;

    /// <summary>Gets the serialized property name.</summary>
    public string PropertyName { get; }
}
