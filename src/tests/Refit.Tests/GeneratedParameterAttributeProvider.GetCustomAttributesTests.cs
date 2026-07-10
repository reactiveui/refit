// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>GeneratedParameterAttributeProvider tests.</summary>
public partial class GeneratedParameterAttributeProviderTests
{
    /// <summary>Test throws ArgumentNullException for null type.</summary>
    [Test]
    public void GetCustomAttributesThrowsForNullType()
    {
        var provider = new GeneratedParameterAttributeProvider(new Dictionary<Type, object[]>());

#nullable disable
        _ = Assert.Throws<ArgumentNullException>(() => _ = provider.GetCustomAttributes(null, false));
#nullable restore
    }

    /// <summary>Test GetCustomAttributes returns an empty array for empty provider.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetCustomAttributesReturnsEmptyArrayForEmptyProvider()
    {
        var provider = new GeneratedParameterAttributeProvider(new Dictionary<Type, object[]>());

        var result = provider.GetCustomAttributes(typeof(QueryAttribute), false);

        await Assert.That(result).IsEmpty();
    }

    /// <summary>Test GetCustomAttributes returns an array.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetCustomAttributesReturnsAttributesArray()
    {
        var provider =
            new GeneratedParameterAttributeProvider(new Dictionary<Type, object[]>
            {
                { typeof(QueryAttribute), [new QueryAttribute()] }
            });

        var result = provider.GetCustomAttributes(typeof(QueryAttribute), false);

        await Assert.That(result).IsNotEmpty().And.HasSingleItem().And.ContainsOnly(static o => o is QueryAttribute);
    }

    /// <summary>Test GetCustomAttributes with no type returns an array of all attributes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetCustomAttributesWithNoTypeReturnsAttributesArray()
    {
        var provider =
            new GeneratedParameterAttributeProvider(new Dictionary<Type, object[]>
            {
                { typeof(QueryAttribute), [new QueryAttribute()] },
                { typeof(AliasAsAttribute), [new AliasAsAttribute("foo")] }
            });

        const int expectedCount = 2;

        var result = provider.GetCustomAttributes(false);

        await Assert.That(result).IsNotEmpty().And.HasCountBetween(expectedCount, expectedCount).And.ContainsOnly(static o => o is QueryAttribute or AliasAsAttribute);
    }
}
