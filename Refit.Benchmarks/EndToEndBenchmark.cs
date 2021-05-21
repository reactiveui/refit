using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AutoFixture;
using BenchmarkDotNet.Attributes;

namespace Refit.Benchmarks
{
    [MemoryDiagnoser]
    public class EndToEndBenchmark
    {
        private readonly Fixture autoFixture = new();
        private const string Host = "https://github.com";
        private SystemTextJsonContentSerializer systemTextJsonContentSerializer;
        private NewtonsoftJsonContentSerializer newtonsoftJsonContentSerializer;
        private readonly IDictionary<int, IEnumerable<User>> users = new Dictionary<int, IEnumerable<User>>();
        private readonly IDictionary<SerializationStrategy, IDictionary<HttpStatusCode ,IGitHubService>> refitClient = new Dictionary<SerializationStrategy, IDictionary<HttpStatusCode ,IGitHubService>>
        {
            {SerializationStrategy.SystemTextJson, new Dictionary<HttpStatusCode, IGitHubService>()},
            {SerializationStrategy.NewtonsoftJson, new Dictionary<HttpStatusCode, IGitHubService>()}
        };

        private readonly IDictionary<HttpVerb, HttpMethod> httpMethod = new Dictionary<HttpVerb, HttpMethod>
        {
            {HttpVerb.Get, HttpMethod.Get}, {HttpVerb.Post, HttpMethod.Post}
        };

        private const int TenUsers = 10;

        public enum SerializationStrategy
        {
            SystemTextJson,
            NewtonsoftJson
        }

        public enum HttpVerb
        {
            Get,
            Post
        }

        [GlobalSetup]
        public async Task SetupAsync()
        {

            systemTextJsonContentSerializer = new SystemTextJsonContentSerializer();
            refitClient[SerializationStrategy.SystemTextJson][HttpStatusCode.OK] = RestService.For<IGitHubService>(Host, new RefitSettings(systemTextJsonContentSerializer)
            {
                HttpMessageHandlerFactory = () => new StaticFileHttpResponseHandler("system-text-json-10-users.json", HttpStatusCode.OK)
            });
            refitClient[SerializationStrategy.SystemTextJson][HttpStatusCode.InternalServerError] = RestService.For<IGitHubService>(Host, new RefitSettings(systemTextJsonContentSerializer)
            {
                HttpMessageHandlerFactory = () => new StaticFileHttpResponseHandler("system-text-json-10-users.json", HttpStatusCode.InternalServerError)
            });

            newtonsoftJsonContentSerializer = new NewtonsoftJsonContentSerializer();
            refitClient[SerializationStrategy.NewtonsoftJson][HttpStatusCode.OK] = RestService.For<IGitHubService>(Host, new RefitSettings(newtonsoftJsonContentSerializer)
            {
                HttpMessageHandlerFactory = () => new StaticFileHttpResponseHandler("newtonsoft-json-10-users.json", System.Net.HttpStatusCode.OK)
            });
            refitClient[SerializationStrategy.NewtonsoftJson][HttpStatusCode.InternalServerError] = RestService.For<IGitHubService>(Host, new RefitSettings(newtonsoftJsonContentSerializer)
            {
                HttpMessageHandlerFactory = () => new StaticFileHttpResponseHandler("newtonsoft-json-10-users.json", System.Net.HttpStatusCode.InternalServerError)
            });

            users[TenUsers] = autoFixture.CreateMany<User>(TenUsers);
        }

        /*
         * Each [Benchmark] tests one return type that Refit allows and is parameterized to test different, serializers, and http methods, and status codes
         */

        [Params(HttpStatusCode.OK, HttpStatusCode.InternalServerError)]
        public HttpStatusCode HttpStatusCode { get; set; }

        [Params(TenUsers)]
        public int ModelCount { get; set; }

        [ParamsAllValues]
        public HttpVerb Verb { get; set; }

        [ParamsAllValues]
        public SerializationStrategy Serializer { get; set; }

        [Benchmark]
        public async Task Task_Async()
        {
            try
            {
                switch (Verb)
                {
                    case HttpVerb.Get:
                        await refitClient[Serializer][HttpStatusCode].GetUsersTaskAsync();
                        break;
                    case HttpVerb.Post:
                        await refitClient[Serializer][HttpStatusCode].PostUsersTaskAsync(users[ModelCount]);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Verb));
                }
            }
            catch
            {
                //swallow
            }
        }

        [Benchmark]
        public async Task<string> TaskString_Async()
        {
            try
            {
                switch (Verb)
                {
                    case HttpVerb.Get:
                        return await refitClient[Serializer][HttpStatusCode].GetUsersTaskStringAsync();
                    case HttpVerb.Post:
                        return await refitClient[Serializer][HttpStatusCode].PostUsersTaskStringAsync(users[ModelCount]);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Verb));
                }
            }
            catch
            {
                //swallow
            }

            return default;
        }

        [Benchmark]
        public async Task<Stream> TaskStream_Async()
        {
            try
            {
                switch (Verb)
                {
                    case HttpVerb.Get:
                        return await refitClient[Serializer][HttpStatusCode].GetUsersTaskStreamAsync();
                    case HttpVerb.Post:
                        return await refitClient[Serializer][HttpStatusCode].PostUsersTaskStreamAsync(users[ModelCount]);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Verb));
                }
            }
            catch
            {
                //swallow
            }

            return default;
        }

        [Benchmark]
        public async Task<HttpContent> TaskHttpContent_Async()
        {
            try
            {
                switch (Verb)
                {
                    case HttpVerb.Get:
                        return await refitClient[Serializer][HttpStatusCode].GetUsersTaskHttpContentAsync();
                    case HttpVerb.Post:
                        return await refitClient[Serializer][HttpStatusCode].PostUsersTaskHttpContentAsync(users[ModelCount]);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Verb));
                }
            }
            catch
            {
                //swallow
            }

            return default;
        }

        [Benchmark]
        public async Task<HttpResponseMessage> TaskHttpResponseMessage_Async()
        {
            switch (Verb)
            {
                case HttpVerb.Get:
                    return await refitClient[Serializer][HttpStatusCode].GetUsersTaskHttpResponseMessageAsync();
                case HttpVerb.Post:
                    return await refitClient[Serializer][HttpStatusCode].PostUsersTaskHttpResponseMessageAsync(users[ModelCount]);
                default:
                    throw new ArgumentOutOfRangeException(nameof(Verb));
            }
        }

        [Benchmark]
        public IObservable<HttpResponseMessage> ObservableHttpResponseMessage()
        {
            switch (Verb)
            {
                case HttpVerb.Get:
                    return refitClient[Serializer][HttpStatusCode].GetUsersObservableHttpResponseMessage();
                case HttpVerb.Post:
                    return refitClient[Serializer][HttpStatusCode].PostUsersObservableHttpResponseMessage(users[ModelCount]);
                default:
                    throw new ArgumentOutOfRangeException(nameof(Verb));
            }
        }

        [Benchmark]
        public async Task<List<User>> TaskT_Async()
        {
            try
            {
                switch (Verb)
                {
                    case HttpVerb.Get:
                        return await refitClient[Serializer][HttpStatusCode].GetUsersTaskTAsync();
                    case HttpVerb.Post:
                        return await refitClient[Serializer][HttpStatusCode].PostUsersTaskTAsync(users[ModelCount]);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Verb));
                }
            }
            catch
            {
                //swallow
            }

            return default;
        }

        [Benchmark]
        public async Task<ApiResponse<List<User>>> TaskApiResponseT_Async()
        {
            switch (Verb)
            {
                case HttpVerb.Get:
                    return await refitClient[Serializer][HttpStatusCode].GetUsersTaskApiResponseTAsync();
                case HttpVerb.Post:
                    return await refitClient[Serializer][HttpStatusCode].PostUsersTaskApiResponseTAsync(users[ModelCount]);
                default:
                    throw new ArgumentOutOfRangeException(nameof(Verb));
            }
        }
    }
}
