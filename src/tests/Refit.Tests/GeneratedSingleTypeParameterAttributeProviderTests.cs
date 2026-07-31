// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Tests for the attribute provider holding attributes of a single type.</summary>
public class GeneratedSingleTypeParameterAttributeProviderTests
{
    /// <summary>Test GetCustomAttributes throws ArgumentNullException for a null type.</summary>
    [Test]
    public void GetCustomAttributesThrowsForNullType()
    {
        var provider = new GeneratedSingleTypeParameterAttributeProvider(typeof(QueryAttribute), [new QueryAttribute()]);

#nullable disable
        _ = Assert.Throws<ArgumentNullException>(() => _ = provider.GetCustomAttributes(null, false));
#nullable restore
    }

    /// <summary>Test IsDefined throws ArgumentNullException for a null type.</summary>
    [Test]
    public void IsDefinedThrowsForNullType()
    {
        var provider = new GeneratedSingleTypeParameterAttributeProvider(typeof(QueryAttribute), [new QueryAttribute()]);

#nullable disable
        _ = Assert.Throws<ArgumentNullException>(() => _ = provider.IsDefined(null, false));
#nullable restore
    }

    /// <summary>Test GetCustomAttributes returns every attribute for the declared type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetCustomAttributesReturnsAttributesForDeclaredType()
    {
        const int ExpectedCount = 2;
        var provider = new GeneratedSingleTypeParameterAttributeProvider(
            typeof(QueryAttribute),
            [new QueryAttribute(), new QueryAttribute()]);

        var result = provider.GetCustomAttributes(typeof(QueryAttribute), false);

        await Assert.That(result).Count().IsEqualTo(ExpectedCount);
        await Assert.That(result).ContainsOnly(static o => o is QueryAttribute);
    }

    /// <summary>Test GetCustomAttributes returns an empty array for any other type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetCustomAttributesReturnsEmptyArrayForOtherType()
    {
        var provider = new GeneratedSingleTypeParameterAttributeProvider(typeof(QueryAttribute), [new QueryAttribute()]);

        var result = provider.GetCustomAttributes(typeof(AliasAsAttribute), false);

        await Assert.That(result).IsEmpty();
    }

    /// <summary>Test GetCustomAttributes with no type returns the attributes without flattening.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetCustomAttributesWithNoTypeReturnsAllAttributes()
    {
        object[] attributes = [new QueryAttribute()];
        var provider = new GeneratedSingleTypeParameterAttributeProvider(typeof(QueryAttribute), attributes);

        var result = provider.GetCustomAttributes(false);

        await Assert.That(result).IsSameReferenceAs(attributes);
    }

    /// <summary>Test IsDefined only reports the declared attribute type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsDefinedReportsOnlyTheDeclaredType()
    {
        var provider = new GeneratedSingleTypeParameterAttributeProvider(typeof(QueryAttribute), [new QueryAttribute()]);

        await Assert.That(provider.IsDefined(typeof(QueryAttribute), false)).IsTrue();
        await Assert.That(provider.IsDefined(typeof(AliasAsAttribute), false)).IsFalse();
    }
}
