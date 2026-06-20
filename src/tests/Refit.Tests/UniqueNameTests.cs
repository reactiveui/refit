// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Generic;

namespace Refit.Tests;

/// <summary>Tests that <see cref="T:Refit.UniqueName"/> generates stable, collision-free names for types.</summary>
public class UniqueNameTests
{
    /// <summary>
    /// Verifies that requesting the unique name for the same type twice yields the same value, even when
    /// a system type name collides with a language keyword alias.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SystemTypeAndLanguageTypeHaveSameNames()
    {
        var name1 = UniqueName.ForType<int>();
        var name2 = UniqueName.ForType<int>();

        await Assert.That(name2).IsEqualTo(name1);
    }

    /// <summary>Verifies a generic type closed over different arguments produces distinct unique names.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task GenericClassWithDifferentTypesHaveUniqueNames()
    {
        var name1 = UniqueName.ForType<List<long>>();
        var name2 = UniqueName.ForType<List<int>>();

        await Assert.That(name2).IsNotEqualTo(name1);
    }

    /// <summary>Verifies same-named types in different namespaces produce distinct unique names.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SameClassNameInDifferentNamespacesHaveUniqueNames()
    {
        var name1 = UniqueName.ForType<Http.Client>();
        var name2 = UniqueName.ForType<Tcp.Client>();

        await Assert.That(name2).IsNotEqualTo(name1);
    }

    /// <summary>Verifies an outer type and its nested type produce distinct unique names.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ClassesWithNestedClassesHaveUniqueNames()
    {
        var name1 = UniqueName.ForType<Http.Client>();
        var name2 = UniqueName.ForType<Http.Client.Request>();

        await Assert.That(name2).IsNotEqualTo(name1);
    }

    /// <summary>Verifies sibling nested types under the same outer type produce distinct unique names.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NestedClassesHaveUniqueNames()
    {
        var name1 = UniqueName.ForType<Http.Client.Request>();
        var name2 = UniqueName.ForType<Http.Client.Response>();

        await Assert.That(name2).IsNotEqualTo(name1);
    }

    /// <summary>Verifies service keys are included only when present.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ServiceKeysAreIncludedOnlyWhenPresent()
    {
        var withoutKey = UniqueName.ForType<Http.Client>(null);
        var emptyKey = UniqueName.ForType<Http.Client>(string.Empty);
        var withKey = UniqueName.ForType<Http.Client>("primary");

        await Assert.That(emptyKey).IsEqualTo(withoutKey);
        await Assert.That(withKey).Contains("ServiceKey=primary");
    }
}
