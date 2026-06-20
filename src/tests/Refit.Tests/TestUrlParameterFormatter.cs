// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit.Tests;

/// <summary>A URL parameter formatter that always returns a constant value.</summary>
public class TestUrlParameterFormatter : IUrlParameterFormatter
{
    /// <summary>The constant value returned for every parameter.</summary>
    private readonly string _constantParameterOutput;

    /// <summary>Initializes a new instance of the <see cref="TestUrlParameterFormatter"/> class.</summary>
    /// <param name="constantOutput">The constant value to return for every parameter.</param>
    public TestUrlParameterFormatter(string constantOutput) => _constantParameterOutput = constantOutput;

    /// <summary>Returns the configured constant value.</summary>
    /// <param name="value">The parameter value to format.</param>
    /// <param name="attributeProvider">The attribute provider for the parameter.</param>
    /// <param name="type">The declared type of the parameter.</param>
    /// <returns>The configured constant value.</returns>
    public string Format(object? value, ICustomAttributeProvider attributeProvider, Type type) => _constantParameterOutput;
}
