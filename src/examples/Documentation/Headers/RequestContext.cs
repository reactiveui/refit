// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Security.Cryptography;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Checks all built-in context keys and authorization constructor variants.</summary>
internal static class RequestContext
{
    /// <summary>The key used to demonstrate context precedence.</summary>
    private const string TenantKey = "tenant";

    /// <summary>The number of declared SaveAsync arguments, including its cancellation token.</summary>
    private const int ArgumentCount = 3;

    /// <summary>Test-only bytes used as encoded authorization data for the local handler.</summary>
    private static readonly string TestToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(ArgumentCount));

    /// <summary>Checks context at construction and body capture during sending.</summary>
    /// <param name="host">Provides generated JSON metadata without modifying shared settings.</param>
    /// <returns>A task that completes after the locally handled request is checked.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        RefitSettings settings = new(host.Settings.ContentSerializer)
        {
            CaptureMethodArguments = true,
            CaptureRequestContent = true,
            HttpRequestMessageOptions = new() { [TenantKey] = "settings-tenant" },
        };
        IContextApi api = RestService.ForGenerated<IContextApi>(host.Client, settings);
        api.Tenant = "client-tenant";

        using HttpRequestMessage built = await api.BuildAsync(TestToken, null);
        Console.WriteLine(built.Headers.Authorization); // Basic followed by the test token
        Console.WriteLine(built.Headers.Contains("X-App")); // False
        _ = built.Options.TryGetValue(new(TenantKey), out string? clientTenant);
        Console.WriteLine(clientTenant); // client-tenant

        SampleCheck.Equal($"Basic {TestToken}", built.Headers.Authorization?.ToString());
        SampleCheck.Equal(false, built.Headers.Contains("X-App"));
        SampleCheck.Equal("client-tenant", clientTenant);
        HttpRequestMessage? sent = null;
        host.Http.Add(new() { Method = HttpMethod.Post, Template = "/context" }, Reply.From(request =>
        {
            sent = request;
            return new(System.Net.HttpStatusCode.NoContent);
        }));
        await api.SaveAsync(new(1, "Ada"), "call-tenant", CancellationToken.None);
        ReadSentContext(sent!);
        await host.Http.VerifyAllCalledAsync();
    }

    /// <summary>Checks generated metadata and send-time body capture observed by a handler.</summary>
    /// <param name="request">The request already sent through the local handler.</param>
    private static void ReadSentContext(HttpRequestMessage request)
    {
        _ = request.Options.TryGetValue(new(HttpRequestMessageOptions.InterfaceType), out Type? interfaceType);
        bool hasReflectedInfo = request.Options.TryGetValue(new(HttpRequestMessageOptions.RestMethodInfo), out object? reflectedInfo);
        _ = request.Options.TryGetValue(new(HttpRequestMessageOptions.MethodArguments), out object?[]? arguments);
        _ = request.Options.TryGetValue(new(HttpRequestMessageOptions.RequestContent), out string? body);
        _ = request.Options.TryGetValue(new(TenantKey), out string? tenant);
        Console.WriteLine(interfaceType == typeof(IContextApi)); // True
        Console.WriteLine(hasReflectedInfo); // False for this generated method
        Console.WriteLine(arguments?.Length); // 3, including CancellationToken
        Console.WriteLine(body); // JSON: {"id":1,"name":"Ada"}.
        Console.WriteLine(tenant); // call-tenant

        SampleCheck.Equal(typeof(IContextApi), interfaceType);
        SampleCheck.Equal(false, hasReflectedInfo);
        SampleCheck.Equal(null, reflectedInfo);
        SampleCheck.Equal(ArgumentCount, arguments?.Length);
        SampleCheck.Equal("{\"id\":1,\"name\":\"Ada\"}", body);
        SampleCheck.Equal("call-tenant", tenant);
    }
}
