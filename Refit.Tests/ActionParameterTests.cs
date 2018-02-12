using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Refit;
using RichardSzalay.MockHttp;
using Xunit;

namespace Refit.Tests
{
    public interface IActionOrFuncParameterTest
    {
        [Get("")]
        Task<HttpResponseMessage> Get([Body]ActionOrFuncOptions options);

        [Get("")]
        Task<HttpResponseMessage> Get([Body]Action<ActionOrFuncOptions> options);

        [Get("")]
        Task<HttpResponseMessage> Get([Body]Func<ActionOrFuncOptions> builder);
    }

    public class ActionOrFuncOptions
    {
        public string Text { get; set; }
        public int Number { get; set; }
        public Boolean Enabled { get; set; }
    }

    public class ActionOrFuncOptionsBuilder
    {
        private ActionOrFuncOptions options = new ActionOrFuncOptions();

        public ActionOrFuncOptionsBuilder WithText(string text)
        {
            options.Text = text;
            return this;
        }

        public ActionOrFuncOptionsBuilder WithNumber(int number)
        {
            options.Number = number;
            return this;
        }

        public ActionOrFuncOptionsBuilder Enabled(bool value)
        {
            options.Enabled = value;
            return this;
        }


        public static implicit operator ActionOrFuncOptions(ActionOrFuncOptionsBuilder builder)
        {
            return builder.options;
        }

    }



    public class ActionParameterTests
    {

        private ActionOrFuncOptions _options { get; } = new ActionOrFuncOptions
        {
            Text = "TextValue",
            Number = 7,
            Enabled = true
        };


        [Fact]
        public async Task StandardParameterTest()
        {

            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/")
                .WithContent(JsonConvert.SerializeObject(_options))
                .Respond(HttpStatusCode.OK, "text/html", "OK");

            
            var fixture = RestService.For<IActionOrFuncParameterTest>("https://httpbin.org/", settings);

            var response = await fixture.Get(_options);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        }

        [Fact]
        public async Task ActionParameterTest()
        {

            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/")
                .WithContent(JsonConvert.SerializeObject(_options))
                .Respond(HttpStatusCode.OK, "text/html", "OK");


            var fixture = RestService.For<IActionOrFuncParameterTest>("https://httpbin.org/", settings);

            var response = await fixture.Get(testOptions =>
            {
                testOptions.Text = "TextValue";
                testOptions.Enabled = true;
                testOptions.Number = 7;
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        }

        [Fact]
        public async Task FuncParameterTest()
        {

            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/")
                .WithContent(JsonConvert.SerializeObject(_options))
                .Respond(HttpStatusCode.OK, "text/html", "OK");

            
            var fixture = RestService.For<IActionOrFuncParameterTest>("https://httpbin.org/", settings);

            var response = await fixture.Get(() => new ActionOrFuncOptionsBuilder().WithNumber(7).WithText("TextValue").Enabled(true));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        }

    }
}
