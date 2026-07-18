// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

    /// <summary>Verifies a type declared in the global namespace produces a unique name without a namespace segment.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload", Justification = "Test intentionally exercises the non-generic Type overload with a namespace-less type.")]
    public async Task GlobalNamespaceTypeProducesUniqueName()
    {
        var name = UniqueName.ForType(typeof(IGlobalNamespaceRefitApi));

        await Assert.That(name).Contains(nameof(IGlobalNamespaceRefitApi));
    }

    /// <summary>Verifies a null or empty assembly name yields no scope, keeping the historical container name.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task MissingAssemblyNameProducesNoScope()
    {
        await Assert.That(UniqueName.SanitizeAssemblyName(null)).IsEqualTo(string.Empty);
        await Assert.That(UniqueName.SanitizeAssemblyName(string.Empty)).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies a lowercase assembly name is folded in with its case preserved (no forced Pascal-casing).</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task LowercaseAssemblyNameIsPreserved()
    {
        await Assert.That(UniqueName.SanitizeAssemblyName("myapp")).IsEqualTo("myapp");
    }

    /// <summary>Verifies characters that cannot appear in an identifier, such as dots and dashes, become underscores.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NonIdentifierCharactersBecomeUnderscores()
    {
        await Assert.That(UniqueName.SanitizeAssemblyName("Refit.Tests-1")).IsEqualTo("Refit_Tests_1");
    }

    /// <summary>Verifies a leading digit is preserved; the container base name prefixes it, so the result stays valid.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task LeadingDigitAssemblyNameIsPreserved()
    {
        await Assert.That(UniqueName.SanitizeAssemblyName("7zip")).IsEqualTo("7zip");
    }
}
