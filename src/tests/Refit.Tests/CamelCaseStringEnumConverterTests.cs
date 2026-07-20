// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Pins the serialized-name maps the camelCase enum converter builds from an enum's fields, so the build path
/// stays behavior-preserving under allocation refactoring.</summary>
public sealed class CamelCaseStringEnumConverterTests
{
    /// <summary>The expected names-to-values entry count: two entries each for <c>DateDescending</c> and <c>Name</c>
    /// (camelCase plus declared name) and one for the already-lowercase <c>plain</c>.</summary>
    private const int ExpectedNameEntryCount = 5;

    /// <summary>The camelCase preferred name of <see cref="SortOrder.DateDescending"/>.</summary>
    private const string DateDescendingCamelCase = "dateDescending";

    /// <summary>An enum whose members exercise the camelCase preferred name plus the declared-name fallback.</summary>
    private enum SortOrder
    {
        /// <summary>A multi-word member whose camelCase name ("dateDescending") differs from its declared name.</summary>
        DateDescending,

        /// <summary>A single-word member whose camelCase name ("name") differs from its declared name.</summary>
        Name,

        /// <summary>An already-lowercase member whose camelCase name equals its declared name, contributing one entry.</summary>
        plain,
    }

    /// <summary>Verifies the names-to-values map carries both the camelCase preferred name and the declared name for a
    /// member whose two names differ, and a single entry for an already-lowercase member.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NamesToValuesMapCarriesCamelCaseAndDeclaredNames()
    {
        var map = CamelCaseStringEnumConverter.EnumConverter<SortOrder>.BuildNameMaps().NamesToValues;

        await Assert.That(map[DateDescendingCamelCase]).IsEqualTo(SortOrder.DateDescending);
        await Assert.That(map["DateDescending"]).IsEqualTo(SortOrder.DateDescending);
        await Assert.That(map["name"]).IsEqualTo(SortOrder.Name);
        await Assert.That(map["Name"]).IsEqualTo(SortOrder.Name);
        await Assert.That(map["plain"]).IsEqualTo(SortOrder.plain);

        // "plain" is already lowercase, so its camelCase and declared names coincide and it contributes one entry only.
        await Assert.That(map.Count).IsEqualTo(ExpectedNameEntryCount);
    }

    /// <summary>Verifies the ordinal-ignore-case map resolves a name regardless of case, while the ordinal map is
    /// case-sensitive, matching the two lookups the converter performs.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NamesToValuesMapsHonorComparerCaseSensitivity()
    {
        var maps = CamelCaseStringEnumConverter.EnumConverter<SortOrder>.BuildNameMaps();

        await Assert.That(maps.NamesToValues.ContainsKey("DATEDESCENDING")).IsFalse();
        await Assert.That(maps.NamesToValuesIgnoreCase["DATEDESCENDING"]).IsEqualTo(SortOrder.DateDescending);
    }

    /// <summary>Verifies a constructed converter builds its ordinal names-to-values map from the enum's fields, so the
    /// entry count it exposes matches the map produced by <see cref="CamelCaseStringEnumConverter.EnumConverter{TEnum}.BuildNameMaps"/>.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ConstructedConverterMapEntryCountMatchesBuiltNameMap()
    {
        var converter = new CamelCaseStringEnumConverter.EnumConverter<SortOrder>();

        await Assert.That(converter.MapEntryCount).IsEqualTo(ExpectedNameEntryCount);
    }

    /// <summary>Verifies the values-to-names map yields each value's camelCase preferred name.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ValuesToNamesMapYieldsPreferredCamelCaseName()
    {
        var map = CamelCaseStringEnumConverter.EnumConverter<SortOrder>.BuildNameMaps().ValuesToNames;

        await Assert.That(map[SortOrder.DateDescending]).IsEqualTo(DateDescendingCamelCase);
        await Assert.That(map[SortOrder.Name]).IsEqualTo("name");
        await Assert.That(map[SortOrder.plain]).IsEqualTo("plain");
    }

    /// <summary>Verifies camelCase conversion lowercases only the first character and returns non-uppercase-leading and
    /// empty inputs unchanged.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ToCamelCaseLowercasesOnlyTheFirstCharacter()
    {
        await Assert.That(CamelCaseStringEnumConverter.ToCamelCase("DateDescending")).IsEqualTo(DateDescendingCamelCase);
        await Assert.That(CamelCaseStringEnumConverter.ToCamelCase("A")).IsEqualTo("a");
        await Assert.That(CamelCaseStringEnumConverter.ToCamelCase("ABC")).IsEqualTo("aBC");
        await Assert.That(CamelCaseStringEnumConverter.ToCamelCase("already")).IsEqualTo("already");
        await Assert.That(CamelCaseStringEnumConverter.ToCamelCase("_leading")).IsEqualTo("_leading");
        await Assert.That(CamelCaseStringEnumConverter.ToCamelCase(string.Empty)).IsEqualTo(string.Empty);
    }
}
