using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using Refit; // for the code gen
using Xunit;

namespace Refit.Tests {
    public class TestAliasObject
    {
        [AliasAs("FIELD_WE_SHOULD_SHORTEN_WITH_ALIAS_AS")]
        public string ShortNameForAlias { get; set; }

        [JsonProperty(PropertyName = "FIELD_WE_SHOULD_SHORTEN_WITH_JSON_PROPERTY")]
        public string ShortNameForJsonProperty { get; set; }
    }

    public class AliasAsResponseTests
    {
        public interface IMyAliasService
        {
            [Get("/aliasTest")]
            Task<TestAliasObject> GetTestObject();
        }

        [Fact]
        public async Task JsonPropertyCanBeUsedToAliasFieldNames()
        {
            var mockHandler = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHandler
            };

            mockHandler.Expect(HttpMethod.Get, "http://api/aliasTest")
                .Respond("application/json", "{FIELD_WE_SHOULD_SHORTEN_WITH_ALIAS_AS: 'Hello', FIELD_WE_SHOULD_SHORTEN_WITH_JSON_PROPERTY: 'World'}");

            var fixture = RestService.For<IMyAliasService>("http://api", settings);

            var result = await fixture.GetTestObject();

            Assert.Equal("World", result.ShortNameForJsonProperty);
        }

        [Fact]
        public async Task AliasAsCanBeUsedToAliasFieldNames()
        {
            var mockHandler = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHandler
            };

            mockHandler.Expect(HttpMethod.Get, "http://api/aliasTest")
                .Respond("application/json", "{FIELD_WE_SHOULD_SHORTEN_WITH_ALIAS_AS: 'Hello', FIELD_WE_SHOULD_SHORTEN_WITH_JSON_PROPERTY: 'World'}");

            var fixture = RestService.For<IMyAliasService>("http://api", settings);

            var result = await fixture.GetTestObject();

            Assert.Equal("Hello", result.ShortNameForAlias);
        }

    }
}
