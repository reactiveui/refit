using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;

namespace Refit.Profiler
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.NetCoreApp21)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    public class RefitBenchmark
    {
        private IGitHubService newtonsoftJsonFixture;
        private IGitHubService systemTextJsonFixture;

        [GlobalSetup]
        public void Setup()
        {
            var mockHttp = new MockHttpMessageHandler();

            var newtonsoftJsonSettings = new RefitSettings(new NewtonsoftJsonContentSerializer())
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            this.newtonsoftJsonFixture = RestService.For<IGitHubService>("http://github.com", newtonsoftJsonSettings);

            var systemTextJsonSettings = new RefitSettings(new SystemTextJsonContentSerializer())
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            this.systemTextJsonFixture = RestService.For<IGitHubService>("http://github.com", systemTextJsonSettings);

            var user = new User
            {
                Id = 123456789,
                Name = "refit",
                Bio = "The automatic type-safe REST library for .NET Core, Xamarin and .NET. Heavily inspired by Square's Retrofit library, Refit turns your REST API into a live interface. https://reactiveui.github.io/refit/",
                Followers = 3900,
                Following = 170,
                Url = "https://github.com/reactiveui/refit"
            };

            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(user))
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/problem+json")
                    }
                }
            };

            mockHttp
                .Expect(HttpMethod.Get, "http://github.com/users/refit.json")
                .Respond(_ => expectedResponse);
        }

        [Benchmark(Baseline = true)]
        public Task<User> GetUserWithNewtonsoftJsonAsync()
        {
            return this.newtonsoftJsonFixture.GetUserAsync("refit");
        }

        [Benchmark]
        public Task<User> GetUserWithSystemTextJsonAsync()
        {
            return this.systemTextJsonFixture.GetUserAsync("refit");
        }
    }
}
