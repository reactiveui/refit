// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>Tests for <see cref="FormValueMultimap"/> source loading and serialization behavior.</summary>
public partial class FormValueMultimapTests
{
    /// <summary>The second element shared by the sample integer collections.</summary>
    private const int SecondSampleInt = 2;

    /// <summary>Comma-delimited field value shared by the repeated-field tests.</summary>
    private const string CommaDelimitedValue = "set1,set2";

    /// <summary>Space-delimited field value shared by the repeated-field tests.</summary>
    private const string SpaceDelimitedValue = "01 02";

    /// <summary>Tab-delimited field value shared by the repeated-field tests.</summary>
    private const string TabDelimitedValue = "0.10\t1.00";

    /// <summary>Pipe-delimited boolean field value shared by the repeated-field tests.</summary>
    private const string PipeDelimitedBooleanValue = "True|False";

    /// <summary>The third element in the sample integer collection.</summary>
    private const int ThirdSampleInt = 3;

    /// <summary>The first element in the sample double collection.</summary>
    private const double FirstSampleDouble = 0.1;

    /// <summary>The second element in the sample double collection.</summary>
    private const double SecondSampleDouble = 1.0;

    /// <summary>Sample scalar name value shared by the nested-flattening fixtures.</summary>
    private const string SampleName = "ada";

    /// <summary>Field key for the shared scalar name value.</summary>
    private const string NameFieldKey = "Name";

    /// <summary>Nested email value shared by the nested-flattening fixtures.</summary>
    private const string NestedEmail = "a@b.com";

    /// <summary>Scalar value shared by the cycle-guard fixture.</summary>
    private const string CycleRootValue = "root";

    /// <summary>The object-graph depth beyond which the flattener drops deeper nested values; mirrors <c>FormValueMultimap.MaxNestingDepth</c>.</summary>
    private const int MaxNestingDepth = 32;

    /// <summary>Extra nodes appended past the nesting cap so the depth-cap drop path is exercised with margin.</summary>
    private const int ExtraNodesBeyondNestingCap = 5;

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
        await Assert.That(static () => new FormValueMultimap(new(), null!))
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
            A = [1, SecondSampleInt],
            B = new HashSet<string> { "set1", "set2" },
            C = [1, SecondSampleInt],
            D = [FirstSampleDouble, SecondSampleDouble],
            E = [true, false]
        };
        var expected = new List<KeyValuePair<string?, string?>>
        {
            new("A", "01"),
            new("A", "02"),
            new("B", CommaDelimitedValue),
            new("C", SpaceDelimitedValue),
            new("D", TabDelimitedValue),

            // The default behavior is to capitalize booleans. This is not a requirement.
            new("E", PipeDelimitedBooleanValue)
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
            A = [1, SecondSampleInt],
            B = new HashSet<string> { "set1", "set2" },
            C = [1, SecondSampleInt],
            D = [FirstSampleDouble, SecondSampleDouble],
            E = [true, false],

            // Member has no explicit CollectionFormat
            F = [1, SecondSampleInt, ThirdSampleInt]
        };
        var expected = new List<KeyValuePair<string?, string?>>
        {
            new("A", "01"),
            new("A", "02"),
            new("B", CommaDelimitedValue),
            new("C", SpaceDelimitedValue),
            new("D", TabDelimitedValue),
            new("E", PipeDelimitedBooleanValue),
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
            A = [1, SecondSampleInt],
            B = new HashSet<string> { "set1", "set2" },
            C = [1, SecondSampleInt],
            D = [FirstSampleDouble, SecondSampleDouble],
            E = [true, false],

            // Member has no explicit CollectionFormat
            F = [1, SecondSampleInt, ThirdSampleInt]
        };
        var expected = new List<KeyValuePair<string?, string?>>
        {
            new("A", "01"),
            new("A", "02"),
            new("B", CommaDelimitedValue),
            new("C", SpaceDelimitedValue),
            new("D", TabDelimitedValue),
            new("E", PipeDelimitedBooleanValue),
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
        const int xyzValue = 123;
        var source = new { foo = "bar", xyz = xyzValue };

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
        await Assert.That(target.FirstOrDefault(static entry => entry.Key == "f").Value).IsEqualTo("abc");
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
        await Assert.That(target.FirstOrDefault(static entry => entry.Key == "b").Value).IsEqualTo("xyz");
    }

    /// <summary>Verifies the <see cref="QueryAttribute"/> prefix and format are applied to a property's key and value.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UsesQueryPropertyAttribute()
    {
        const int frobValue = 4;
        var source = new AliasingTestClass { Frob = frobValue };

        var target = new FormValueMultimap(source, _settings);

        await Assert.That(target.Keys).DoesNotContain("Bar");
        await Assert.That(target.Keys).Contains("prefix-fr");
        await Assert.That(target.FirstOrDefault(static entry => entry.Key == "prefix-fr").Value).IsEqualTo("4.0");
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
        await Assert.That(target.FirstOrDefault(static entry => entry.Key == "a").Value).IsEqualTo("123");
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

    /// <summary>Verifies a nested complex property flattens to dotted parent.child keys.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NestedObjectFlattensToDottedKeys()
    {
        const int age = 42;
        var source = new ObjectWithNestedDetail
        {
            Name = SampleName,
            Detail = new() { Email = NestedEmail, Age = age }
        };
        var expected = new KeyValuePair<string?, string?>[]
        {
            new(NameFieldKey, SampleName),
            new("Detail.Email", NestedEmail),
            new("Detail.Age", "42")
        };

        var actual = new FormValueMultimap(source, _settings);

        await Assert.That(actual).IsCollectionEqualTo(expected);
    }

    /// <summary>Verifies a null nested complex property is omitted rather than emitting an empty or type-name value.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NullNestedObjectIsOmitted()
    {
        var source = new ObjectWithNestedDetail { Name = SampleName, Detail = null };
        var expected = new KeyValuePair<string?, string?>[] { new(NameFieldKey, SampleName) };

        var actual = new FormValueMultimap(source, _settings);

        await Assert.That(actual).IsCollectionEqualTo(expected);
    }

    /// <summary>Verifies a dictionary-typed property flattens to prefixed field.key entries instead of its type name.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DictionaryPropertyFlattensToPrefixedKeys()
    {
        var source = new ObjectWithDictionary
        {
            Name = SampleName,
            Extra = new() { { "key", "val" } }
        };
        var expected = new KeyValuePair<string?, string?>[]
        {
            new(NameFieldKey, SampleName),
            new("Extra.key", "val")
        };

        var actual = new FormValueMultimap(source, _settings);

        await Assert.That(actual).IsCollectionEqualTo(expected);
    }

    /// <summary>Verifies a dictionary of complex values recurses so each entry expands under field.key.child.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DictionaryWithComplexValuesFlattensRecursively()
    {
        const int age = 42;
        var source = new ObjectWithComplexDictionary
        {
            Items = new() { { "first", new() { Email = NestedEmail, Age = age } } }
        };
        var expected = new KeyValuePair<string?, string?>[]
        {
            new("Items.first.Email", NestedEmail),
            new("Items.first.Age", "42")
        };

        var actual = new FormValueMultimap(source, _settings);

        await Assert.That(actual).IsCollectionEqualTo(expected);
    }

    /// <summary>Verifies a plain collection under the default format joins its elements with commas instead of emitting its type name.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CollectionUnderDefaultFormatJoinsWithCommas()
    {
        var source = new ObjectWithDefaultCollection { Numbers = [1, SecondSampleInt, ThirdSampleInt] };
        var expected = new KeyValuePair<string?, string?>[] { new("Numbers", "1,2,3") };

        var actual = new FormValueMultimap(source, _settings);

        await Assert.That(actual).IsCollectionEqualTo(expected);
    }

    /// <summary>Verifies a nested property honors its <see cref="QueryAttribute"/> prefix/delimiter and nested <see cref="AliasAsAttribute"/> when composing keys.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NestedPrefixAndAliasComposeKeys()
    {
        var source = new ObjectWithPrefixedNested
        {
            Detail = new() { Zip = "1010", City = "Wien" }
        };
        var expected = new KeyValuePair<string?, string?>[]
        {
            new("addr-Detail-z", "1010"),
            new("addr-Detail-City", "Wien")
        };

        var actual = new FormValueMultimap(source, _settings);

        await Assert.That(actual).IsCollectionEqualTo(expected);
    }

    /// <summary>Verifies a self-referential model flattens to a bounded set of entries rather than overflowing the stack.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SelfReferentialObjectStopsAtCycleWithoutOverflow()
    {
        var source = new SelfReferentialForm { Value = CycleRootValue };
        source.Self = source;
        var expected = new KeyValuePair<string?, string?>[]
        {
            new("Value", CycleRootValue),
            new("Self.Value", CycleRootValue)
        };

        var actual = new FormValueMultimap(source, _settings);

        await Assert.That(actual).IsCollectionEqualTo(expected);
    }

    /// <summary>Verifies an acyclic object graph deeper than the nesting cap flattens the values up to the cap and drops everything below it.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DeeplyNestedObjectDropsValuesBeyondNestingCap()
    {
        var source = BuildDeepChain(MaxNestingDepth + ExtraNodesBeyondNestingCap);

        var values = new FormValueMultimap(source, _settings)
            .Select(static entry => entry.Value)
            .ToArray();

        await Assert.That(values.Length).IsEqualTo(MaxNestingDepth + 1);
        await Assert.That(values.Contains(DeepNodeValue(MaxNestingDepth))).IsTrue();
        await Assert.That(values.Contains(DeepNodeValue(MaxNestingDepth + 1))).IsFalse();
    }

    /// <summary>Verifies a nested object inheriting a null <see cref="QueryAttribute"/> delimiter falls back to the default delimiter when composing its own children.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NestedObjectUnderNullQueryDelimiterFallsBackToDefaultDelimiter()
    {
        const int age = 42;
        var source = new ObjectWithNullDelimiterQuery
        {
            Detail = new() { Email = NestedEmail, Age = age }
        };
        var expected = new KeyValuePair<string?, string?>[]
        {
            new("DetailEmail", NestedEmail),
            new("DetailAge", "42")
        };

        var actual = new FormValueMultimap(source, _settings);

        await Assert.That(actual).IsCollectionEqualTo(expected);
    }

    /// <summary>Projects a sequence of non-nullable key/value pairs to nullable key/value pairs.</summary>
    /// <param name="source">The non-nullable key/value pairs to project.</param>
    /// <returns>The equivalent sequence with nullable key and value types.</returns>
    private static IEnumerable<KeyValuePair<string?, string?>> ToNullableKvps(
        IEnumerable<KeyValuePair<string, string>> source) =>
        source.Select(static pair => new KeyValuePair<string?, string?>(pair.Key, pair.Value));

    /// <summary>Builds an acyclic chain of distinct <see cref="DeepNode"/> instances, each carrying a depth-tagged value.</summary>
    /// <param name="length">The number of nodes in the chain.</param>
    /// <returns>The head node of the chain.</returns>
    private static DeepNode BuildDeepChain(int length)
    {
        var head = new DeepNode { Value = DeepNodeValue(0) };
        var current = head;
        for (var i = 1; i < length; i++)
        {
            var next = new DeepNode { Value = DeepNodeValue(i) };
            current.Child = next;
            current = next;
        }

        return head;
    }

    /// <summary>Formats the value carried by the <see cref="DeepNode"/> at the given depth.</summary>
    /// <param name="depth">The zero-based depth of the node.</param>
    /// <returns>The depth-tagged value.</returns>
    private static string DeepNodeValue(int depth) => "v" + depth.ToString(CultureInfo.InvariantCulture);

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
        /// <summary>An unsupported <see cref="CollectionFormat"/> value used to exercise the unknown-format path.</summary>
        private const int UnknownCollectionFormat = 123;

        /// <summary>The second sample value in the values collection.</summary>
        private const int SecondValue = 2;

        /// <summary>Gets the values collection.</summary>
        [Query((CollectionFormat)UnknownCollectionFormat)]
        public int[] Values { get; } = [1, SecondValue];
    }

    /// <summary>Test fixture whose properties have non-public getters to verify they are excluded.</summary>
    public class ClassWithInaccessibleGetters
    {
        /// <summary>Gets or sets the first value, whose getter is internal and therefore inaccessible to serialization.</summary>
        public string? A { internal get; set; }

        /// <summary>Gets or sets the second value, whose getter is private and therefore inaccessible to serialization.</summary>
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

    /// <summary>Test fixture with a nested complex property that flattens to dotted keys.</summary>
    public class ObjectWithNestedDetail
    {
        /// <summary>Gets or sets the scalar name value.</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the nested detail flattened under its property name.</summary>
        public NestedDetail? Detail { get; set; }
    }

    /// <summary>Nested detail object flattened by the enclosing fixtures.</summary>
    public class NestedDetail
    {
        /// <summary>Gets or sets the email value.</summary>
        public string? Email { get; set; }

        /// <summary>Gets or sets the age value.</summary>
        public int Age { get; set; }
    }

    /// <summary>Test fixture with a dictionary-typed property that flattens to prefixed entry keys.</summary>
    public class ObjectWithDictionary
    {
        /// <summary>Gets or sets the scalar name value.</summary>
        public string? Name { get; set; }

        /// <summary>Gets the extra values flattened under their property name.</summary>
        public Dictionary<string, string>? Extra { get; init; }
    }

    /// <summary>Test fixture with a dictionary of complex values that recurses into each entry.</summary>
    public class ObjectWithComplexDictionary
    {
        /// <summary>Gets the items keyed by name and flattened per entry.</summary>
        public Dictionary<string, NestedDetail>? Items { get; init; }
    }

    /// <summary>Test fixture with a collection property carrying no explicit collection format.</summary>
    public class ObjectWithDefaultCollection
    {
        /// <summary>Gets the numbers joined using the default collection format.</summary>
        public List<int>? Numbers { get; init; }
    }

    /// <summary>Test fixture whose nested property carries a <see cref="QueryAttribute"/> prefix and delimiter.</summary>
    public class ObjectWithPrefixedNested
    {
        /// <summary>Gets or sets the nested detail whose keys compose under the query prefix and delimiter.</summary>
        [Query("-", "addr")]
        public PrefixedNestedDetail? Detail { get; set; }
    }

    /// <summary>Nested detail with an aliased property used to verify nested key composition.</summary>
    public class PrefixedNestedDetail
    {
        /// <summary>Gets or sets the postal code renamed via <see cref="AliasAsAttribute"/>.</summary>
        [AliasAs("z")]
        public string? Zip { get; set; }

        /// <summary>Gets or sets the city value.</summary>
        public string? City { get; set; }
    }

    /// <summary>Test fixture that references itself to exercise the cycle guard.</summary>
    public class SelfReferentialForm
    {
        /// <summary>Gets or sets the scalar value.</summary>
        public string? Value { get; set; }

        /// <summary>Gets or sets the self reference that must not cause unbounded recursion.</summary>
        public SelfReferentialForm? Self { get; set; }
    }

    /// <summary>Recursive test fixture used to build an acyclic graph deeper than the nesting cap.</summary>
    public class DeepNode
    {
        /// <summary>Gets or sets the depth-tagged scalar value flattened at this level.</summary>
        public string? Value { get; set; }

        /// <summary>Gets or sets the next distinct node in the chain, or <see langword="null"/> at the tail.</summary>
        public DeepNode? Child { get; set; }
    }

    /// <summary>Test fixture whose nested property carries a <see cref="QueryAttribute"/> with a null delimiter, exercising the default-delimiter fallback for its children.</summary>
    public class ObjectWithNullDelimiterQuery
    {
        /// <summary>Gets or sets the nested detail whose children inherit the null query delimiter.</summary>
        [Query(null!)]
        public NestedDetail? Detail { get; set; }
    }
}
