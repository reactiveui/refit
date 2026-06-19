// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Reflection;

namespace Refit;

/// <summary>Describes a parameter of a REST method.</summary>
public class RestMethodParameterInfo
{
    /// <summary>Initializes a new instance of the <see cref="RestMethodParameterInfo"/> class with a name.</summary>
    /// <param name="name">The name.</param>
    /// <param name="parameterInfo">The parameter information.</param>
    public RestMethodParameterInfo(string name, ParameterInfo parameterInfo)
    {
        Name = name;
        ParameterInfo = parameterInfo;
    }

    /// <summary>Initializes a new instance of the <see cref="RestMethodParameterInfo"/> class for an object property parameter.</summary>
    /// <param name="isObjectPropertyParameter">if set to <c>true</c> [is object property parameter].</param>
    /// <param name="parameterInfo">The parameter information.</param>
    public RestMethodParameterInfo(bool isObjectPropertyParameter, ParameterInfo parameterInfo)
    {
        IsObjectPropertyParameter = isObjectPropertyParameter;
        ParameterInfo = parameterInfo;
    }

    /// <summary>Gets or sets the name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the parameter information.</summary>
    public ParameterInfo ParameterInfo { get; set; }

    /// <summary>Gets or sets a value indicating whether this instance is an object property parameter.</summary>
    public bool IsObjectPropertyParameter { get; set; }

    /// <summary>Gets the parameter properties.</summary>
    public List<RestMethodParameterProperty> ParameterProperties { get; init; } = [];

    /// <summary>Gets or sets the parameter type.</summary>
    public ParameterType Type { get; set; } = ParameterType.Normal;
}
