using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace Refit.Tests
{
    public class FormValueDictionaryTests
    {
        [Fact]
        public void EmptyIfNullPassedIn()
        {
            var target = new FormValueDictionary(null);
            Assert.Empty(target);
        }


        [Fact]
        public void LoadsFromDictionary()
        {
            var source = new Dictionary<string, string> {
                { "foo", "bar" },
                { "xyz", "123" }
            };

            var target = new FormValueDictionary(source);

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
                { "C", "" }
            };

            var actual = new FormValueDictionary(source);

            Assert.Equal(expected, actual);
        }

        public class ObjectTestClass
        {
            public string A { get; set; }
            public string B { get; set; }
            public string C { get; set; }
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

            var actual = new FormValueDictionary(source);


            Assert.Equal(expected, actual);
        }

        [Fact]
        public void UsesAliasAsAttribute()
        {
            var source = new AliasingTestClass
            {
                Foo = "abc"
            };

            var target = new FormValueDictionary(source);

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

            var target = new FormValueDictionary(source);

            Assert.DoesNotContain("Bar", target.Keys);
            Assert.Contains("b", target.Keys);
            Assert.Equal("xyz", target["b"]);
        }

        [Fact]
        public void GivesPrecedenceToAliasAs()
        {
            var source = new AliasingTestClass
            {
                Baz = "123"
            };

            var target = new FormValueDictionary(source);

            Assert.DoesNotContain("Bar", target.Keys);
            Assert.DoesNotContain("z", target.Keys);
            Assert.Contains("a", target.Keys);
            Assert.Equal("123", target["a"]);
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
        }
    }
}
