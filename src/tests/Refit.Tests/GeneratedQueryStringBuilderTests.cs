// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies the null-omission and collection-delimiter behavior of <see cref="GeneratedQueryStringBuilder"/>.</summary>
public sealed class GeneratedQueryStringBuilderTests
{
    /// <summary>The relative path shared by the builder fixtures.</summary>
    private const string Path = "/x";

    /// <summary>The query key shared by the builder fixtures.</summary>
    private const string Key = "k";

    /// <summary>Verifies a null value omits its parameter, leaving the path unchanged.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AddOmitsParameterForNullValue()
    {
        var result = AddValue(Key, null);

        await Assert.That(result).IsEqualTo(Path);
    }

    /// <summary>Verifies a null flag name omits the flag, leaving the path unchanged.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AddFlagOmitsNullFlagName()
    {
        var result = AddNullFlag();

        await Assert.That(result).IsEqualTo(Path);
    }

    /// <summary>Verifies a space-separated collection joins its values with a space.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BeginCollectionJoinsSpaceSeparatedValues()
    {
        var result = JoinCollection(CollectionFormat.Ssv);

        await Assert.That(result).IsEqualTo("/x?k=a%20b");
    }

    /// <summary>Verifies a tab-separated collection joins its values with a tab.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BeginCollectionJoinsTabSeparatedValues()
    {
        var result = JoinCollection(CollectionFormat.Tsv);

        await Assert.That(result).IsEqualTo("/x?k=a%09b");
    }

    /// <summary>Appends a single value and returns the built relative path.</summary>
    /// <param name="name">The query key.</param>
    /// <param name="value">The value, or null to omit the parameter.</param>
    /// <returns>The built relative path.</returns>
    private static string AddValue(string name, string? value)
    {
        var builder = new GeneratedQueryStringBuilder(Path);
        builder.Add(name, value, false);
        return builder.Build();
    }

    /// <summary>Appends a null flag and returns the built relative path.</summary>
    /// <returns>The built relative path.</returns>
    private static string AddNullFlag()
    {
        var builder = new GeneratedQueryStringBuilder(Path);
        builder.AddFlag(null, false);
        return builder.Build();
    }

    /// <summary>Joins a two-element collection under the given format and returns the built relative path.</summary>
    /// <param name="collectionFormat">The resolved collection format.</param>
    /// <returns>The built relative path.</returns>
    private static string JoinCollection(CollectionFormat collectionFormat)
    {
        var builder = new GeneratedQueryStringBuilder(Path);
        builder.BeginCollection(Key, collectionFormat, false);
        builder.AddCollectionValue("a");
        builder.AddCollectionValue("b");
        builder.EndCollection();
        return builder.Build();
    }
}
