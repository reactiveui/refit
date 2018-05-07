using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace Refit.Tests
{
    public class FormValueDictionaryTests
    {
        readonly RefitSettings settings = new RefitSettings();

        [Fact]
        public void EmptyIfNullPassedIn()
        {
            var target = new FormValueDictionary(null, settings);
            Assert.Empty(target);
        }


        [Fact]
        public void LoadsFromDictionary()
        {
            var source = new Dictionary<string, string> {
                { "foo", "bar" },
                { "xyz", "123" }
            };

            var target = new FormValueDictionary(source, settings);

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

            var actual = new FormValueDictionary(source, settings);

            Assert.Equal(expected, actual);
        }

        public class ObjectTestClass
        {
            public string A { get; set; }
            public string B { get; set; }
            public string C { get; set; }
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

            var actual = new FormValueDictionary(source, settings);

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

            var actual = new FormValueDictionary(source, settings);


            Assert.Equal(expected, actual);
        }

        [Fact]
        public void UsesAliasAsAttribute()
        {
            var source = new AliasingTestClass
            {
                Foo = "abc"
            };

            var target = new FormValueDictionary(source, settings);

            Assert.DoesNotContain("Foo", target.Keys);
            Assert.Contains("f", target.Keys);
            Assert.Equal("abc", target["f"]);
        }

        [Fact]
        public void UsesJsonPropertyAttribute()
        {
            var source = new AliasingTestClass
            {
                Bar = "xyz"
            };

            var target = new FormValueDictionary(source, settings);

            Assert.DoesNotContain("Bar", target.Keys);
            Assert.Contains("b", target.Keys);
            Assert.Equal("xyz", target["b"]);
        }

        [Fact]
        public void UsesQueryPropertyAttribute()
        {
            var source = new AliasingTestClass
            {
                Frob = 4
            };

            var target = new FormValueDictionary(source, settings);

            Assert.DoesNotContain("Bar", target.Keys);
            Assert.Contains("prefix-fr", target.Keys);
            Assert.Equal("4.0", target["prefix-fr"]);
        }


        [Fact]
        public void GivesPrecedenceToAliasAs()
        {
            var source = new AliasingTestClass
            {
                Baz = "123"
            };

            var target = new FormValueDictionary(source, settings);

            Assert.DoesNotContain("Bar", target.Keys);
            Assert.DoesNotContain("z", target.Keys);
            Assert.Contains("a", target.Keys);
            Assert.Equal("123", target["a"]);
        }


        [Fact]
        public void SkipsNullValuesFromDictionary()
        {
            var source = new Dictionary<string, string> {
                { "foo", "bar" },
                { "xyz", null }
            };

            var target = new FormValueDictionary(source, settings);

            Assert.Single(target);
            Assert.Contains("foo", target.Keys);
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
    }
}
