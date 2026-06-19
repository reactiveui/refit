// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Refit.Tests;

/// <summary>A URL parameter formatter that asserts each invocation receives the expected attribute provider and type.</summary>
[RequiresUnreferencedCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
[RequiresDynamicCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
public class TestUrlFormatter : IUrlParameterFormatter
{
    /// <summary>The attribute providers expected on each invocation, in order.</summary>
    private readonly ICustomAttributeProvider[] _expectedAttributeProviders;

    /// <summary>The types expected on each invocation, in order.</summary>
    private readonly Type[] _expectedTypes;

    /// <summary>The zero-based index of the next expected invocation.</summary>
    private int _index;

    /// <summary>Initializes a new instance of the <see cref="TestUrlFormatter"/> class expecting a single invocation.</summary>
    /// <param name="expectedAttributeProvider">The attribute provider expected on the single invocation.</param>
    /// <param name="expectedType">The type expected on the single invocation.</param>
    public TestUrlFormatter(ICustomAttributeProvider expectedAttributeProvider, Type expectedType)
    {
        _expectedAttributeProviders = [expectedAttributeProvider];
        _expectedTypes = [expectedType];
    }

    /// <summary>Initializes a new instance of the <see cref="TestUrlFormatter"/> class expecting a sequence of invocations.</summary>
    /// <param name="expectedAttributeProviders">The attribute providers expected, in invocation order.</param>
    /// <param name="expectedTypes">The types expected, in invocation order.</param>
    public TestUrlFormatter(
        ICustomAttributeProvider[] expectedAttributeProviders,
        Type[] expectedTypes)
    {
        _expectedAttributeProviders = expectedAttributeProviders;
        _expectedTypes = expectedTypes;
    }

    /// <summary>Formats the value, asserting the attribute provider and type match the expectations for this invocation.</summary>
    /// <param name="value">The value being formatted.</param>
    /// <param name="attributeProvider">The attribute provider associated with the value.</param>
    /// <param name="type">The declared type of the value.</param>
    /// <returns>The string representation of the value.</returns>
    public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type)
    {
        if (!Equals(attributeProvider, _expectedAttributeProviders[_index]))
        {
            throw new InvalidOperationException(
                $"Unexpected attribute provider at index {_index}.");
        }

        if (type != _expectedTypes[_index])
        {
            throw new InvalidOperationException($"Unexpected type at index {_index}.");
        }

        _index++;
        return value?.ToString();
    }

    /// <summary>Asserts that the formatter received exactly the number of invocations it expected.</summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    public async Task AssertNoOutstandingAssertions()
    {
        await Assert.That(_index).IsEqualTo(_expectedAttributeProviders.Length);
        await Assert.That(_index).IsEqualTo(_expectedTypes.Length);
    }
}
