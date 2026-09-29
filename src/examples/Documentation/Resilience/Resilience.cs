// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Adds Microsoft's standard resilience handler to a generated client and checks what it retries.</summary>
internal static class Resilience
{
    /// <summary>The service address the registered clients use.</summary>
    private const string BaseUrl = "https://people.example";

    /// <summary>Runs each resilience scenario against local replies.</summary>
    /// <returns>A task that faults when an observed result differs from the documented behavior.</returns>
    internal static async Task RunAsync()
    {
        await RetryReadAsync();
        await SkipUnsafeMethodAsync();
        await SkipBadJsonAsync();
        await RefreshTokenPerAttemptAsync();
    }

    /// <summary>Retries only safe methods, with short waits and explicit timeouts.</summary>
    /// <param name="options">The standard pipeline's options.</param>
    internal static void ConfigureRetries(HttpStandardResilienceOptions options)
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromMilliseconds(100);
        options.Retry.DisableForUnsafeHttpMethods(); // POST, PUT, PATCH, DELETE and CONNECT are sent once
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5); // limits each try
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20); // limits every try and wait together
    }

    /// <summary>Problem: How do you retry a read when the service is briefly unavailable?</summary>
    /// <returns>A task that completes after the retried read is checked.</returns>
    private static async Task RetryReadAsync()
    {
        using StubHttp http = new StubHttp
        {
            { Route.Get("/people/1"), Reply.Status(HttpStatusCode.ServiceUnavailable) },
            { Route.Get("/people/1"), Reply.Json("{\"id\":1,\"name\":\"Ada\"}") },
        };

        ServiceCollection services = new ServiceCollection();
        _ = services.AddRefitGeneratedClient<IResilientPeopleApi>(SampleJsonContext.Default)
            .ConfigureHttpClient(static client => client.BaseAddress = new Uri(BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => http)
            .AddStandardResilienceHandler(ConfigureRetries);

        await using ServiceProvider provider = services.BuildServiceProvider();
        IResilientPeopleApi api = provider.GetRequiredService<IResilientPeopleApi>();

        Person person = await api.GetPersonAsync(1, CancellationToken.None); // the first try gets 503; the retry gets Ada
        Console.WriteLine(person.Name); // Ada
        Console.WriteLine(http.Requests.Count); // 2

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(new Person(1, "Ada"), person);
        SampleCheck.Equal(2, http.Requests.Count);
        await http.VerifyAllCalledAsync();
    }

    /// <summary>Problem: Does the retry send a POST again after a failure?</summary>
    /// <returns>A task that completes after the single POST is checked.</returns>
    private static async Task SkipUnsafeMethodAsync()
    {
        using StubHttp http = new StubHttp
        {
            { Route.Post("/people"), Reply.Status(HttpStatusCode.ServiceUnavailable) },
        };
        await using ServiceProvider provider = CreateProvider(http);
        IResilientPeopleApi api = provider.GetRequiredService<IResilientPeopleApi>();

        HttpStatusCode? status = null;
        try
        {
            _ = await api.CreatePersonAsync(new Person(2, "Grace"), CancellationToken.None);
        }
        catch (ApiException error)
        {
            status = error.StatusCode; // ServiceUnavailable: the POST was sent once and not retried
        }

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(HttpStatusCode.ServiceUnavailable, status);
        SampleCheck.Equal(1, http.Requests.Count);
    }

    /// <summary>Problem: Does the retry handle a reply Refit cannot read?</summary>
    /// <returns>A task that completes after the unread reply is checked.</returns>
    private static async Task SkipBadJsonAsync()
    {
        using StubHttp http = new StubHttp
        {
            { Route.Get("/people/3"), Reply.Json("not JSON") },
        };
        await using ServiceProvider provider = CreateProvider(http);
        IResilientPeopleApi api = provider.GetRequiredService<IResilientPeopleApi>();

        bool failed = false;
        try
        {
            _ = await api.GetPersonAsync(3, CancellationToken.None);
        }
        catch (ApiException)
        {
            failed = true; // the 200 reply passed the resilience handler; Refit failed to read its body afterwards
        }

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(true, failed);
        SampleCheck.Equal(1, http.Requests.Count);
    }

    /// <summary>Problem: How do you get a current token for every retry?</summary>
    /// <returns>A task that completes after both tries' tokens are checked.</returns>
    private static async Task RefreshTokenPerAttemptAsync()
    {
        List<string?> tokensSent = []; // the token each try carried when it reached the service
        using StubHttp http = new StubHttp
        {
            {
                Route.Get("/people/1"),
                Reply.From(request =>
                {
                    tokensSent.Add(request.Headers.Authorization?.Parameter);
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                })
            },
            {
                Route.Get("/people/1"),
                Reply.From(request =>
                {
                    tokensSent.Add(request.Headers.Authorization?.Parameter);
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":1,\"name\":\"Ada\"}", System.Text.Encoding.UTF8, "application/json") };
                })
            },
        };
        int tokensIssued = 0;

        ServiceCollection services = new ServiceCollection();
        IHttpClientBuilder builder = services.AddRefitGeneratedClient<IResilientPeopleApi>(SampleJsonContext.Default)
            .ConfigureHttpClient(static client => client.BaseAddress = new Uri(BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => http);
        _ = builder.AddStandardResilienceHandler(ConfigureRetries); // added first: the outer handler, which repeats what follows
        _ = builder.AddAuthorizationHeaderValueProvider((_, _, _) =>
            ValueTask.FromResult($"token-{Interlocked.Increment(ref tokensIssued)}")); // added second: runs again for every try

        await using ServiceProvider provider = services.BuildServiceProvider();
        _ = await provider.GetRequiredService<IResilientPeopleApi>().GetPersonAsync(1, CancellationToken.None);

        Console.WriteLine(string.Join(", ", tokensSent)); // token-1, token-2: the retry asked for a new token

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal("token-1, token-2", string.Join(", ", tokensSent));
        SampleCheck.Equal(2, tokensIssued);
    }

    /// <summary>Registers the resilient client over a local handler.</summary>
    /// <param name="http">The local handler that answers the client's requests.</param>
    /// <returns>The service provider that owns the client.</returns>
    private static ServiceProvider CreateProvider(StubHttp http)
    {
        ServiceCollection services = new ServiceCollection();
        _ = services.AddRefitGeneratedClient<IResilientPeopleApi>(SampleJsonContext.Default)
            .ConfigureHttpClient(static client => client.BaseAddress = new Uri(BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => http)
            .AddStandardResilienceHandler(ConfigureRetries);
        return services.BuildServiceProvider();
    }
}
