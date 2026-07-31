// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Tests for the attribute provider keyed by attribute type.</summary>
public partial class GeneratedParameterAttributeProviderTests
{
    /// <summary>Test IsDefined returns false for empty provider.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsDefinedReturnsFalseForEmptyProvider()
    {
        var provider = new GeneratedParameterAttributeProvider(new Dictionary<Type, object[]>());

        var isDefined = provider.IsDefined(typeof(QueryAttribute), false);

        await Assert.That(isDefined).IsFalse();
    }

    /// <summary>Test the shared empty provider reports no attributes at all.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmptyProviderReportsNoAttributes()
    {
        var provider = GeneratedParameterAttributeProvider.Empty;

        await Assert.That(provider.IsDefined(typeof(QueryAttribute), false)).IsFalse();
        await Assert.That(provider.GetCustomAttributes(typeof(QueryAttribute), false)).IsEmpty();
        await Assert.That(provider.GetCustomAttributes(false)).IsEmpty();
    }

    /// <summary>Test IsDefined returns false when type doesn't exist.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsDefinedReturnsFalseForUnavailableTypeInProvider()
    {
        var provider = new GeneratedParameterAttributeProvider(
            new Dictionary<Type, object[]> { { typeof(AliasAsAttribute), [new AliasAsAttribute("foo")] } });

        var isDefined = provider.IsDefined(typeof(QueryAttribute), false);

        await Assert.That(isDefined).IsFalse();
    }

    /// <summary>Test IsDefined returns true when type exists.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsDefinedReturnsTrueForAvailableTypeInProvider()
    {
        var provider = new GeneratedParameterAttributeProvider(
            new Dictionary<Type, object[]> { { typeof(QueryAttribute), [new QueryAttribute()] } });

        var isDefined = provider.IsDefined(typeof(QueryAttribute), false);

        await Assert.That(isDefined).IsTrue();
    }
}
