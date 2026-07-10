// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using AutoFixture;
using BenchmarkDotNet.Attributes;

namespace Refit.Benchmarks;

/// <summary>Benchmarks the full request and response pipeline for each return type that Refit supports.</summary>
[MemoryDiagnoser]
public class EndToEndBenchmark
{
    /// <summary>The host used for the benchmarked clients.</summary>
    private const string Host = "https://github.com";

    /// <summary>The number of users used in the ten-user benchmarks.</summary>
    private const int TenUsers = 10;

    /// <summary>The fixture used to generate sample user data.</summary>
    private readonly Fixture _autoFixture = new();

    /// <summary>The generated sample users keyed by model count.</summary>
    private readonly Dictionary<int, IEnumerable<User>> _users = [];

    /// <summary>The Refit clients keyed by serialization strategy and status code.</summary>
    private readonly Dictionary<
        SerializationStrategy,
        IDictionary<HttpStatusCode, IGitHubService>> _refitClient = new()
    {
        { SerializationStrategy.SystemTextJson, new Dictionary<HttpStatusCode, IGitHubService>() },
        { SerializationStrategy.NewtonsoftJson, new Dictionary<HttpStatusCode, IGitHubService>() }
    };

    /// <summary>The content serialization strategy used by a benchmark.</summary>
    public enum SerializationStrategy
    {
        /// <summary>Uses the System.Text.Json serializer.</summary>
        SystemTextJson,

        /// <summary>Uses the Newtonsoft.Json serializer.</summary>
        NewtonsoftJson
    }

    /// <summary>The HTTP verb used by a benchmark.</summary>
    public enum HttpVerb
    {
        /// <summary>The HTTP GET verb.</summary>
        Get,

        /// <summary>The HTTP POST verb.</summary>
        Post
    }

    /// <summary>Gets or sets the HTTP status code returned by the benchmarked handler.</summary>
    [Params(HttpStatusCode.OK, HttpStatusCode.InternalServerError)]
    public HttpStatusCode HttpStatusCode { get; set; }

    /// <summary>Gets or sets the number of models used in the benchmark.</summary>
    [Params(TenUsers)]
    public int ModelCount { get; set; }

    /// <summary>Gets or sets the HTTP verb used by the benchmark.</summary>
    [ParamsAllValues]
    public HttpVerb Verb { get; set; }

    /// <summary>Gets or sets the serialization strategy used by the benchmark.</summary>
    [ParamsAllValues]
    public SerializationStrategy Serializer { get; set; }

    /// <summary>Sets up the serializers, clients, and sample data before the benchmarks run.</summary>
    /// <returns>A task that completes when setup has finished.</returns>
    [GlobalSetup]
    public Task SetupAsync()
    {
        var systemTextJsonContentSerializer = new SystemTextJsonContentSerializer();
        _refitClient[SerializationStrategy.SystemTextJson][HttpStatusCode.OK] =
            RestService.For<IGitHubService>(
                Host,
                new(systemTextJsonContentSerializer)
                {
                    HttpMessageHandlerFactory = static () =>
                        new StaticFileHttpResponseHandler(
                            "system-text-json-10-users.json",
                            HttpStatusCode.OK)
                });
        _refitClient[SerializationStrategy.SystemTextJson][HttpStatusCode.InternalServerError] =
            RestService.For<IGitHubService>(
                Host,
                new(systemTextJsonContentSerializer)
                {
                    HttpMessageHandlerFactory = static () =>
                        new StaticFileHttpResponseHandler(
                            "system-text-json-10-users.json",
                            HttpStatusCode.InternalServerError)
                });

        var newtonsoftJsonContentSerializer = new NewtonsoftJsonContentSerializer();
        _refitClient[SerializationStrategy.NewtonsoftJson][HttpStatusCode.OK] =
            RestService.For<IGitHubService>(
                Host,
                new(newtonsoftJsonContentSerializer)
                {
                    HttpMessageHandlerFactory = static () =>
                        new StaticFileHttpResponseHandler(
                            "newtonsoft-json-10-users.json",
                            HttpStatusCode.OK)
                });
        _refitClient[SerializationStrategy.NewtonsoftJson][HttpStatusCode.InternalServerError] =
            RestService.For<IGitHubService>(
                Host,
                new(newtonsoftJsonContentSerializer)
                {
                    HttpMessageHandlerFactory = static () =>
                        new StaticFileHttpResponseHandler(
                            "newtonsoft-json-10-users.json",
                            HttpStatusCode.InternalServerError)
                });

        _users[TenUsers] = _autoFixture.CreateMany<User>(TenUsers);

        return Task.CompletedTask;
    }

    /*
     * Each [Benchmark] tests one return type that Refit allows and is parameterized to test different, serializers, and http methods, and status codes
     */

    /// <summary>Benchmarks a method that returns a plain Task.</summary>
    /// <returns>A task that completes when the request has finished.</returns>
    [Benchmark]
    public async Task Task_Async()
    {
        var client = _refitClient[Serializer][HttpStatusCode];
        try
        {
            await (Verb switch
            {
                HttpVerb.Get => client.GetUsersTaskAsync(),
                HttpVerb.Post => client.PostUsersTaskAsync(_users[ModelCount]),
                _ => throw new ArgumentOutOfRangeException(nameof(Verb))
            });
        }
        catch (ApiException)
        {
            // The InternalServerError parameterization deliberately produces a failing response.
        }
    }

    /// <summary>Benchmarks a method that returns a string response.</summary>
    /// <returns>The string response.</returns>
    [Benchmark]
    public async Task<string?> TaskString_Async()
    {
        var client = _refitClient[Serializer][HttpStatusCode];
        try
        {
            return Verb switch
            {
                HttpVerb.Get => await client.GetUsersTaskStringAsync(),
                HttpVerb.Post => await client.PostUsersTaskStringAsync(_users[ModelCount]),
                _ => throw new ArgumentOutOfRangeException(nameof(Verb))
            };
        }
        catch (ApiException)
        {
            // The InternalServerError parameterization deliberately produces a failing response.
        }

        return default;
    }

    /// <summary>Benchmarks a method that returns a response stream.</summary>
    /// <returns>The response stream.</returns>
    [Benchmark]
    public async Task<Stream?> TaskStream_Async()
    {
        var client = _refitClient[Serializer][HttpStatusCode];
        try
        {
            return Verb switch
            {
                HttpVerb.Get => await client.GetUsersTaskStreamAsync(),
                HttpVerb.Post => await client.PostUsersTaskStreamAsync(_users[ModelCount]),
                _ => throw new ArgumentOutOfRangeException(nameof(Verb))
            };
        }
        catch (ApiException)
        {
            // The InternalServerError parameterization deliberately produces a failing response.
        }

        return default;
    }

    /// <summary>Benchmarks a method that returns the raw HTTP content.</summary>
    /// <returns>The response HTTP content.</returns>
    [Benchmark]
    public async Task<HttpContent?> TaskHttpContent_Async()
    {
        var client = _refitClient[Serializer][HttpStatusCode];
        try
        {
            return Verb switch
            {
                HttpVerb.Get => await client.GetUsersTaskHttpContentAsync(),
                HttpVerb.Post => await client.PostUsersTaskHttpContentAsync(_users[ModelCount]),
                _ => throw new ArgumentOutOfRangeException(nameof(Verb))
            };
        }
        catch (ApiException)
        {
            // The InternalServerError parameterization deliberately produces a failing response.
        }

        return default;
    }

    /// <summary>Benchmarks a method that returns the full HTTP response message.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    public Task<HttpResponseMessage> TaskHttpResponseMessage_Async()
    {
        var client = _refitClient[Serializer][HttpStatusCode];
        return Verb switch
        {
            HttpVerb.Get => client.GetUsersTaskHttpResponseMessageAsync(),
            HttpVerb.Post => client.PostUsersTaskHttpResponseMessageAsync(_users[ModelCount]),
            _ => throw new ArgumentOutOfRangeException(nameof(Verb))
        };
    }

    /// <summary>Benchmarks a method that returns an observable HTTP response message.</summary>
    /// <returns>An observable that produces the HTTP response message.</returns>
    [Benchmark]
    public IObservable<HttpResponseMessage> ObservableHttpResponseMessage()
    {
        var client = _refitClient[Serializer][HttpStatusCode];
        return Verb switch
        {
            HttpVerb.Get => client.GetUsersObservableHttpResponseMessage(),
            HttpVerb.Post => client.PostUsersObservableHttpResponseMessage(_users[ModelCount]),
            _ => throw new ArgumentOutOfRangeException(nameof(Verb))
        };
    }

    /// <summary>Benchmarks a method that returns a deserialized list of users.</summary>
    /// <returns>The deserialized list of users.</returns>
    [Benchmark]
    public async Task<List<User>?> TaskT_Async()
    {
        var client = _refitClient[Serializer][HttpStatusCode];
        try
        {
            return Verb switch
            {
                HttpVerb.Get => await client.GetUsersTaskTAsync(),
                HttpVerb.Post => await client.PostUsersTaskTAsync(_users[ModelCount]),
                _ => throw new ArgumentOutOfRangeException(nameof(Verb))
            };
        }
        catch (ApiException)
        {
            // The InternalServerError parameterization deliberately produces a failing response.
        }

        return default;
    }

    /// <summary>Benchmarks a method that returns an API response wrapping a list of users.</summary>
    /// <returns>The API response wrapping the list of users.</returns>
    [Benchmark]
    public Task<ApiResponse<List<User>>> TaskApiResponseT_Async()
    {
        var client = _refitClient[Serializer][HttpStatusCode];
        return Verb switch
        {
            HttpVerb.Get => client.GetUsersTaskApiResponseTAsync(),
            HttpVerb.Post => client.PostUsersTaskApiResponseTAsync(_users[ModelCount]),
            _ => throw new ArgumentOutOfRangeException(nameof(Verb))
        };
    }
}
