using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

using Newtonsoft.Json;

using Xunit;

namespace Refit.Tests
{
    public class FormValueMultimapTests
    {
        readonly RefitSettings settings = new RefitSettings();

        [Fact]
        public void EmptyIfNullPassedIn()
        {
            var target = new FormValueMultimap(null, settings);
            Assert.Empty(target);
        }


        [Fact]
        public void LoadsFromDictionary()
        {
            var source = new Dictionary<string, string> {
                { "foo", "bar" },
                { "xyz", "123" }
            };

            var target = new FormValueMultimap(source, settings);

            Assert.Equal(source, target);
        }

        [Fact]
        public void LoadsFromObject()
        {
            var source = new ObjectTestClass
            {
                A = "1",
                B = "2"
            };
            var expected = new Dictionary<string, string>
            {
                { "A", "1" },
                { "B", "2" },
            };

            var actual = new FormValueMultimap(source, settings);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void LoadFromObjectWithCollections()
        {
            var source = new ObjectWithRepeatedFieldsTestClass
            {
                A = new List<int> { 1, 2 },
                B = new HashSet<string> { "set1", "set2" },
                C = new HashSet<int> { 1, 2 },
                D = new List<double> { 0.1, 1.0 },
                E = new List<bool> { true, false }
            };
            var expected = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("A", "01"),
                new KeyValuePair<string, string>("A", "02"),
                new KeyValuePair<string, string>("B", "set1,set2"),
                new KeyValuePair<string, string>("C", "01 02"),
                new KeyValuePair<string, string>("D", "0.10\t1.00"),

                // The default behavior is to capitalize booleans. This is not a requirement.
                new KeyValuePair<string, string>("E", "True|False")
            };

            var actual = new FormValueMultimap(source, settings);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DefaultCollectionFormatCanBeSpecifiedInSettings_Multi()
        {
            var settingsWithCollectionFormat = new RefitSettings
            {
                CollectionFormat = CollectionFormat.Multi
            };
            var source = new ObjectWithRepeatedFieldsTestClass
            {
                // Members have explicit CollectionFormat
                A = new List<int> { 1, 2 },
                B = new HashSet<string> { "set1", "set2" },
                C = new HashSet<int> { 1, 2 },
                D = new List<double> { 0.1, 1.0 },
                E = new List<bool> { true, false },

                // Member has no explicit CollectionFormat
                F = new[] { 1, 2, 3 }
            };
            var expected = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("A", "01"),
                new KeyValuePair<string, string>("A", "02"),
                new KeyValuePair<string, string>("B", "set1,set2"),
                new KeyValuePair<string, string>("C", "01 02"),
                new KeyValuePair<string, string>("D", "0.10\t1.00"),
                new KeyValuePair<string, string>("E", "True|False"),
                new KeyValuePair<string, string>("F", "1"),
                new KeyValuePair<string, string>("F", "2"),
                new KeyValuePair<string, string>("F", "3"),
            };

            var actual = new FormValueMultimap(source, settingsWithCollectionFormat);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(CollectionFormat.Csv, "1,2,3")]
        [InlineData(CollectionFormat.Pipes, "1|2|3")]
        [InlineData(CollectionFormat.Ssv, "1 2 3")]
        [InlineData(CollectionFormat.Tsv, "1\t2\t3")]
        public void DefaultCollectionFormatCanBeSpecifiedInSettings(CollectionFormat format, string expectedFormat)
        {
            var settingsWithCollectionFormat = new RefitSettings
            {
                CollectionFormat = format
            };
            var source = new ObjectWithRepeatedFieldsTestClass
            {
                // Members have explicit CollectionFormat
                A = new List<int> { 1, 2 },
                B = new HashSet<string> { "set1", "set2" },
                C = new HashSet<int> { 1, 2 },
                D = new List<double> { 0.1, 1.0 },
                E = new List<bool> { true, false },

                // Member has no explicit CollectionFormat
                F = new[] { 1, 2, 3 }
            };
            var expected = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("A", "01"),
                new KeyValuePair<string, string>("A", "02"),
                new KeyValuePair<string, string>("B", "set1,set2"),
                new KeyValuePair<string, string>("C", "01 02"),
                new KeyValuePair<string, string>("D", "0.10\t1.00"),
                new KeyValuePair<string, string>("E", "True|False"),
                new KeyValuePair<string, string>("F", expectedFormat),
            };

            var actual = new FormValueMultimap(source, settingsWithCollectionFormat);

            Assert.Equal(expected, actual);
        }

        public class ObjectTestClass
        {
            public string A { get; set; }
            public string B { get; set; }
            public string C { get; set; }
        }

        public class ObjectWithRepeatedFieldsTestClass
        {
            [Query(CollectionFormat.Multi, Format = "00")]
            public IList<int> A { get; set; }
            [Query(CollectionFormat.Csv)]
            public ISet<string> B { get; set; }
            [Query(CollectionFormat.Ssv, Format = "00")]
            public HashSet<int> C { get; set; }
            [Query(CollectionFormat.Tsv, Format = "0.00")]
            public IList<double> D { get; set; }
            [Query(CollectionFormat.Pipes)]
            public IList<bool> E { get; set; }
            [Query]
            public int[] F { get; set; }
        }

        [Fact]
        public void ExcludesPropertiesWithInaccessibleGetters()
        {
            var source = new ClassWithInaccessibleGetters
            {
                A = "Foo",
                B = "Bar"
            };
            var expected = new Dictionary<string, string>
            {
                { "C", "FooBar" }
            };

            var actual = new FormValueMultimap(source, settings);

            Assert.Equal(expected, actual);
        }

        public class ClassWithInaccessibleGetters
        {
            public string A { internal get; set; }
            public string B { private get; set; }
            public string C => A + B;
        }

        [Fact]
        public void LoadsFromAnonymousType()
        {
            var source = new
            {
                foo = "bar",
                xyz = 123
            };

            var expected = new Dictionary<string, string>
            {
                { "foo", "bar" },
                { "xyz", "123" }
            };

            var actual = new FormValueMultimap(source, settings);


            Assert.Equal(expected, actual);
        }

        [Fact]
        public void UsesAliasAsAttribute()
        {
            var source = new AliasingTestClass
            {
                Foo = "abc"
            };

            var target = new FormValueMultimap(source, settings);

            Assert.DoesNotContain("Foo", target.Keys);
            Assert.Contains("f", target.Keys);
            Assert.Equal("abc", target.FirstOrDefault(entry => entry.Key == "f").Value);
        }

        [Fact]
        public void UsesJsonPropertyAttribute()
        {
            var source = new AliasingTestClass
            {
                Bar = "xyz"
            };

            var target = new FormValueMultimap(source, settings);

            Assert.DoesNotContain("Bar", target.Keys);
            Assert.Contains("b", target.Keys);
            Assert.Equal("xyz", target.FirstOrDefault(entry => entry.Key == "b").Value);
        }

        [Fact]
        public void UsesQueryPropertyAttribute()
        {
            var source = new AliasingTestClass
            {
                Frob = 4
            };

            var target = new FormValueMultimap(source, settings);

            Assert.DoesNotContain("Bar", target.Keys);
            Assert.Contains("prefix-fr", target.Keys);
            Assert.Equal("4.0", target.FirstOrDefault(entry => entry.Key == "prefix-fr").Value);
        }


        [Fact]
        public void GivesPrecedenceToAliasAs()
        {
            var source = new AliasingTestClass
            {
                Baz = "123"
            };

            var target = new FormValueMultimap(source, settings);

            Assert.DoesNotContain("Bar", target.Keys);
            Assert.DoesNotContain("z", target.Keys);
            Assert.Contains("a", target.Keys);
            Assert.Equal("123", target.FirstOrDefault(entry => entry.Key == "a").Value);
        }


        [Fact]
        public void SkipsNullValuesFromDictionary()
        {
            var source = new Dictionary<string, string> {
                { "foo", "bar" },
                { "xyz", null }
            };

            var target = new FormValueMultimap(source, settings);

            Assert.Single(target);
            Assert.Contains("foo", target.Keys);
        }


        [Fact]
        public void SerializesEnumWithEnumMemberAttribute()
        {
            var source = new Dictionary<string, EnumWithEnumMember>()
            {
                { "A", EnumWithEnumMember.A },
                { "B", EnumWithEnumMember.B }
            };

            var expected = new Dictionary<string, string>
            {
                { "A", "A" },
                { "B", "b" }
            };


            var actual = new FormValueMultimap(source, settings);

            Assert.Equal(expected, actual);
        }


        public class AliasingTestClass
        {
            [AliasAs("f")]
            public string Foo { get; set; }

            [JsonProperty(PropertyName = "b")]
            public string Bar { get; set; }

            [AliasAs("a")]
            [JsonProperty(PropertyName = "z")]
            public string Baz { get; set; }


            [Query("-", "prefix", "0.0")]
            [AliasAs("fr")]
            public int? Frob { get; set; }
        }

        public enum EnumWithEnumMember
        {
            A,

            [EnumMember(Value = "b")]
            B
        }
    }
}
