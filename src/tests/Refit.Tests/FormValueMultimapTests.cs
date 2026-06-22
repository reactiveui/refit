// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>Tests for <see cref="FormValueMultimap"/> source loading and serialization behavior.</summary>
public class FormValueMultimapTests
{
    /// <summary>The default settings used to construct the multimap under test.</summary>
    private readonly RefitSettings _settings = new();

    /// <summary>Enum used to verify that <see cref="EnumMemberAttribute"/> values are honored during serialization.</summary>
    public enum EnumWithEnumMember
    {
        /// <summary>A member serialized using its default name.</summary>
        A,

        /// <summary>A member serialized using its <see cref="EnumMemberAttribute"/> value.</summary>
        [EnumMember(Value = "b")]
        B
    }

    /// <summary>Verifies the multimap is empty when a null source is passed in.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task EmptyIfNullPassedIn()
    {
        var target = new FormValueMultimap(null!, _settings);
        await Assert.That(target).IsEmpty();
    }

    /// <summary>Verifies a null settings instance is rejected before source processing.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RejectsNullSettings() =>
        await Assert.That(() => new FormValueMultimap(new object(), null!))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Verifies the multimap loads entries from a dictionary source.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task LoadsFromDictionary()
    {
        var source = new Dictionary<string, string> { { "foo", "bar" }, { "xyz", "123" } };

        var target = new FormValueMultimap(source, _settings);
        var nonGenericEntries = ((IEnumerable)target).Cast<KeyValuePair<string?, string?>>().ToArray();
        var nonGenericEnumerator = ((IEnumerable)target).GetEnumerator();

        await Assert.That(target).IsCollectionEqualTo(ToNullableKvps(source));
        await Assert.That(nonGenericEntries).IsCollectionEqualTo(ToNullableKvps(source));
        await Assert.That(nonGenericEnumerator.MoveNext()).IsTrue();
    }

    /// <summary>Verifies the generic factory handles null sources without property discovery.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateHandlesNullSources()
    {
        var target = FormValueMultimap.Create<object?>(null, _settings);

        await Assert.That(target).IsEmpty();
    }

    /// <summary>Verifies the multimap loads entries from an object's public properties.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task LoadsFromObject()
    {
        var source = new ObjectTestClass { A = "1", B = "2" };
        var expected = new Dictionary<string, string> { { "A", "1" }, { "B", "2" }, };

        var actual = new FormValueMultimap(source, _settings);

        await Assert.That(actual).IsCollectionEqualTo(ToNullableKvps(expected));
    }

    /// <summary>Verifies collection-typed properties are serialized using their configured formats.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task LoadFromObjectWithCollections()
    {
        var source = new ObjectWithRepeatedFieldsTestClass
        {
            A = [1, 2],
            B = new HashSet<string> { "set1", "set2" },
            C = [1, 2],
            D = [0.1, 1.0],
            E = [true, false]
        };
        var expected = new List<KeyValuePair<string?, string?>>
        {
            new("A", "01"),
            new("A", "02"),
            new("B", "set1,set2"),
            new("C", "01 02"),
            new("D", "0.10\t1.00"),

            // The default behavior is to capitalize booleans. This is not a requirement.
            new("E", "True|False")
        };

        var actual = new FormValueMultimap(source, _settings);

        await Assert.That(actual).IsCollectionEqualTo(expected);
    }

    /// <summary>Verifies the default collection format from settings applies to members without an explicit format, using multi formatting.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DefaultCollectionFormatCanBeSpecifiedInSettings_Multi()
    {
        var settingsWithCollectionFormat = new RefitSettings
        {
            CollectionFormat = CollectionFormat.Multi
        };
        var source = new ObjectWithRepeatedFieldsTestClass
        {
            // Members have explicit CollectionFormat
            A = [1, 2],
            B = new HashSet<string> { "set1", "set2" },
            C = [1, 2],
            D = [0.1, 1.0],
            E = [true, false],

            // Member has no explicit CollectionFormat
            F = [1, 2, 3]
        };
        var expected = new List<KeyValuePair<string?, string?>>
        {
            new("A", "01"),
            new("A", "02"),
            new("B", "set1,set2"),
            new("C", "01 02"),
            new("D", "0.10\t1.00"),
            new("E", "True|False"),
            new("F", "1"),
            new("F", "2"),
            new("F", "3"),
        };

        var actual = new FormValueMultimap(source, settingsWithCollectionFormat);

        await Assert.That(actual).IsCollectionEqualTo(expected);
    }

    /// <summary>Verifies the default collection format from settings applies to members without an explicit format.</summary>
    /// <param name="format">The default collection format to configure in settings.</param>
    /// <param name="expectedFormat">The expected serialized value for the member without an explicit format.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(CollectionFormat.Csv, "1,2,3")]
    [Arguments(CollectionFormat.Pipes, "1|2|3")]
    [Arguments(CollectionFormat.Ssv, "1 2 3")]
    [Arguments(CollectionFormat.Tsv, "1\t2\t3")]
    public async Task DefaultCollectionFormatCanBeSpecifiedInSettings(
        CollectionFormat format,
        string expectedFormat)
    {
        var settingsWithCollectionFormat = new RefitSettings { CollectionFormat = format };
        var source = new ObjectWithRepeatedFieldsTestClass
        {
            // Members have explicit CollectionFormat
            A = [1, 2],
            B = new HashSet<string> { "set1", "set2" },
            C = [1, 2],
            D = [0.1, 1.0],
            E = [true, false],

            // Member has no explicit CollectionFormat
            F = [1, 2, 3]
        };
        var expected = new List<KeyValuePair<string?, string?>>
        {
            new("A", "01"),
            new("A", "02"),
            new("B", "set1,set2"),
            new("C", "01 02"),
            new("D", "0.10\t1.00"),
            new("E", "True|False"),
            new("F", expectedFormat),
        };

        var actual = new FormValueMultimap(source, settingsWithCollectionFormat);

        await Assert.That(actual).IsCollectionEqualTo(expected);
    }

    /// <summary>Verifies an empty delimited collection is represented by an empty value.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task EmptyDelimitedCollectionSerializesAsEmptyValue()
    {
        var source = new ObjectWithEmptyDelimitedCollection();
        var expected = new[]
        {
            new KeyValuePair<string?, string?>("Values", string.Empty)
        };

        var actual = new FormValueMultimap(source, _settings);

        await Assert.That(actual).IsCollectionEqualTo(expected);
    }

    /// <summary>Verifies unknown collection formats fall back to formatting the collection object itself.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UnknownCollectionFormatFallsBackToObjectFormatter()
    {
        var source = new ObjectWithUnknownCollectionFormat();
        var expected = new[]
        {
            new KeyValuePair<string?, string?>("Values", source.Values.ToString())
        };

        var actual = new FormValueMultimap(source, _settings);

        await Assert.That(actual).IsCollectionEqualTo(expected);
    }

    /// <summary>Verifies properties with non-public getters are excluded from the multimap.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ExcludesPropertiesWithInaccessibleGetters()
    {
        var source = new ClassWithInaccessibleGetters { A = "Foo", B = "Bar" };
        var expected = new Dictionary<string, string> { { "C", "FooBar" } };

        var actual = new FormValueMultimap(source, _settings);

        await Assert.That(actual).IsCollectionEqualTo(ToNullableKvps(expected));
    }

    /// <summary>Verifies the multimap loads entries from an anonymous type's properties.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [SuppressMessage(
        "RoslynCommonAnalyzers",
        "SST2224:Convert anonymous type to a tuple",
        Justification = "Verifies loading from an anonymous type; a tuple exposes Item1/Item2 fields, not reflectable named properties.")]
    public async Task LoadsFromAnonymousType()
    {
        var source = new { foo = "bar", xyz = 123 };

        var expected = new Dictionary<string, string> { { "foo", "bar" }, { "xyz", "123" } };

        var actual = new FormValueMultimap(source, _settings);

        await Assert.That(actual).IsCollectionEqualTo(ToNullableKvps(expected));
    }

    /// <summary>Verifies the <see cref="AliasAsAttribute"/> renames a property's key.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UsesAliasAsAttribute()
    {
        var source = new AliasingTestClass { Foo = "abc" };

        var target = new FormValueMultimap(source, _settings);

        await Assert.That(target.Keys).DoesNotContain("Foo");
        await Assert.That(target.Keys).Contains("f");
        await Assert.That(target.FirstOrDefault(entry => entry.Key == "f").Value).IsEqualTo("abc");
    }

    /// <summary>Verifies the <see cref="JsonPropertyNameAttribute"/> renames a property's key.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UsesJsonPropertyAttribute()
    {
        var source = new AliasingTestClass { Bar = "xyz" };

        var target = new FormValueMultimap(source, _settings);

        await Assert.That(target.Keys).DoesNotContain("Bar");
        await Assert.That(target.Keys).Contains("b");
        await Assert.That(target.FirstOrDefault(entry => entry.Key == "b").Value).IsEqualTo("xyz");
    }

    /// <summary>Verifies the <see cref="QueryAttribute"/> prefix and format are applied to a property's key and value.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UsesQueryPropertyAttribute()
    {
        var source = new AliasingTestClass { Frob = 4 };

        var target = new FormValueMultimap(source, _settings);

        await Assert.That(target.Keys).DoesNotContain("Bar");
        await Assert.That(target.Keys).Contains("prefix-fr");
        await Assert.That(target.FirstOrDefault(entry => entry.Key == "prefix-fr").Value).IsEqualTo("4.0");
    }

    /// <summary>Verifies the <see cref="AliasAsAttribute"/> takes precedence over the <see cref="JsonPropertyNameAttribute"/>.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GivesPrecedenceToAliasAs()
    {
        var source = new AliasingTestClass { Baz = "123" };

        var target = new FormValueMultimap(source, _settings);

        await Assert.That(target.Keys).DoesNotContain("Bar");
        await Assert.That(target.Keys).DoesNotContain("z");
        await Assert.That(target.Keys).Contains("a");
        await Assert.That(target.FirstOrDefault(entry => entry.Key == "a").Value).IsEqualTo("123");
    }

    /// <summary>Verifies dictionary entries with null values are skipped.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SkipsNullValuesFromDictionary()
    {
        var source = new Dictionary<string, string?> { { "foo", "bar" }, { "xyz", null } };

        var target = new FormValueMultimap(source, _settings);

        await Assert.That(target).HasSingleItem();
        await Assert.That(target.Keys).Contains("foo");
    }

    /// <summary>Verifies enum values are serialized using their <see cref="EnumMemberAttribute"/> values.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SerializesEnumWithEnumMemberAttribute()
    {
        var source = new Dictionary<string, EnumWithEnumMember>
        {
            { "A", EnumWithEnumMember.A },
            { "B", EnumWithEnumMember.B }
        };

        var expected = new Dictionary<string, string> { { "A", "A" }, { "B", "b" } };

        var actual = new FormValueMultimap(source, _settings);

        await Assert.That(actual).IsCollectionEqualTo(ToNullableKvps(expected));
    }

    /// <summary>Projects a sequence of non-nullable key/value pairs to nullable key/value pairs.</summary>
    /// <param name="source">The non-nullable key/value pairs to project.</param>
    /// <returns>The equivalent sequence with nullable key and value types.</returns>
    private static IEnumerable<KeyValuePair<string?, string?>> ToNullableKvps(
        IEnumerable<KeyValuePair<string, string>> source) =>
        source.Select(static pair => new KeyValuePair<string?, string?>(pair.Key, pair.Value));

    /// <summary>Test fixture with simple string properties used to verify object property loading.</summary>
    public class ObjectTestClass
    {
        /// <summary>Gets or sets the first test value.</summary>
        public string? A { get; set; }

        /// <summary>Gets or sets the second test value.</summary>
        public string? B { get; set; }

        /// <summary>Gets or sets the third test value.</summary>
        public string? C { get; set; }
    }

    /// <summary>Test fixture with collection properties that exercise the various collection formats.</summary>
    public class ObjectWithRepeatedFieldsTestClass
    {
        /// <summary>Gets the integer list formatted using multi collection format.</summary>
        [Query(CollectionFormat.Multi, Format = "00")]
        public IList<int>? A { get; init; }

        /// <summary>Gets the string set formatted using CSV collection format.</summary>
        [Query(CollectionFormat.Csv)]
        public ISet<string>? B { get; init; }

        /// <summary>Gets the integer set formatted using SSV collection format.</summary>
        [Query(CollectionFormat.Ssv, Format = "00")]
        public HashSet<int>? C { get; init; }

        /// <summary>Gets the double list formatted using TSV collection format.</summary>
        [Query(CollectionFormat.Tsv, Format = "0.00")]
        public IList<double>? D { get; init; }

        /// <summary>Gets the boolean list formatted using pipe collection format.</summary>
        [Query(CollectionFormat.Pipes)]
        public IList<bool>? E { get; init; }

        /// <summary>Gets the integer array with no explicit collection format.</summary>
        [Query]
        public int[]? F { get; init; }
    }

    /// <summary>Test fixture with an empty collection using a delimited collection format.</summary>
    public class ObjectWithEmptyDelimitedCollection
    {
        /// <summary>Gets the empty values collection.</summary>
        [Query(CollectionFormat.Csv)]
        public ArrayList Values { get; } = [];
    }

    /// <summary>Test fixture with an unsupported collection format value.</summary>
    public class ObjectWithUnknownCollectionFormat
    {
        /// <summary>Gets the values collection.</summary>
        [Query((CollectionFormat)123)]
        public int[] Values { get; } = [1, 2];
    }

    /// <summary>Test fixture whose properties have non-public getters to verify they are excluded.</summary>
    public class ClassWithInaccessibleGetters
    {
        /// <summary>Gets or sets the first value, whose getter is internal and therefore inaccessible to serialization.</summary>
        [SuppressMessage("Design", "CA1044:Properties should not be write only", Justification = "Intentional inaccessible getter to verify FormValueMultimap excludes it.")]
        public string? A { internal get; set; }

        /// <summary>Gets or sets the second value, whose getter is private and therefore inaccessible to serialization.</summary>
        [SuppressMessage("Design", "CA1044:Properties should not be write only", Justification = "Intentional inaccessible getter to verify FormValueMultimap excludes it.")]
        public string? B { private get; set; }

        /// <summary>Gets the concatenation of the two inaccessible values.</summary>
        public string? C => A + B;
    }

    /// <summary>Test fixture exercising the alias, JSON property, and query attributes used for key naming.</summary>
    public class AliasingTestClass
    {
        /// <summary>Gets or sets the value renamed via <see cref="AliasAsAttribute"/>.</summary>
        [AliasAs("f")]
        public string? Foo { get; set; }

        /// <summary>Gets or sets the value renamed via the JSON property name attribute.</summary>
        [JsonPropertyName("b")]
        public string? Bar { get; set; }

        /// <summary>Gets or sets the value where <see cref="AliasAsAttribute"/> takes precedence over JSON naming.</summary>
        [AliasAs("a")]
        [JsonPropertyName("z")]
        public string? Baz { get; set; }

        /// <summary>Gets or sets the value formatted via <see cref="QueryAttribute"/>.</summary>
        [Query("-", "prefix", "0.0")]
        [AliasAs("fr")]
        public int? Frob { get; set; }
    }
}
