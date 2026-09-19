// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Checks opt-in reflection APIs in an executable that uses runtime compilation.</summary>
internal static class ReflectionClients
{
    /// <summary>The base URL used for clients that are created solely to check overloads.</summary>
    private const string BaseUrl = "https://people.example";

    /// <summary>The service key used to distinguish reflected registrations.</summary>
    private const string ServiceKey = "reflection";

    /// <summary>The local route used to verify reflected sends.</summary>
    private const string PersonRoute = "/clients/person";

    /// <summary>The API interface selected through runtime-Type overloads.</summary>
    private static readonly Type ClientInterface = typeof(IClientApi);

    /// <summary>The disposable interface selected through runtime-Type overloads.</summary>
    private static readonly Type OwnedClientInterface = typeof(IOwnedClientApi);

    /// <summary>The interface selected through untyped reflection builder overloads.</summary>
    private static readonly Type LookupInterface = typeof(IRequestLookupApi);

    /// <summary>Checks reflection creation, delegate selection and factory registration overloads.</summary>
    /// <param name="host">The caller-owned client and generated-JSON settings.</param>
    /// <returns>A task that completes after reflected delegates build their requests.</returns>
    [RequiresUnreferencedCode("These examples explicitly exercise the opt-in reflection request builder.")]
    [RequiresDynamicCode("These examples explicitly close reflected generic methods and DI types.")]
    internal static async Task RunAsync(SampleHost host)
    {
        await CheckReturnAdapterAsync(host);
        IClientApi reflected = RestService.For<IClientApi>(host.Client, host.Settings);
        IRequestBuilder<IClientApi> builder = RequestBuilder.ForType<IClientApi>(host.Settings);
        IClientApi suppliedBuilder = RestService.For(host.Client, builder);
        object runtimeSelected = RestService.For(ClientInterface, host.Client, builder);
        SampleCheck.Equal(true, reflected is IClientApi);
        SampleCheck.Equal(true, suppliedBuilder is IClientApi);
        SampleCheck.Equal(true, runtimeSelected is IClientApi);
        Person expected = new(1, "Ada");
        foreach (IClientApi api in new[] { reflected, suppliedBuilder, (IClientApi)runtimeSelected })
        {
            host.Http.Add(new() { Method = HttpMethod.Get, Template = PersonRoute }, Reply.With(expected));
            SampleCheck.Equal(expected, await api.ReadAsync());
        }

        _ = RestService.For<IClientApi>(host.Client);
        _ = RestService.For(ClientInterface, host.Client);
        _ = RestService.For(ClientInterface, host.Client, host.Settings);
        RefitSettings ownedSettings = new(host.Settings.ContentSerializer) { HttpMessageHandlerFactory = () => host.Http };
        using IOwnedClientApi owned = RestService.For<IOwnedClientApi>(BaseUrl, ownedSettings);
        host.Http.Add(new() { Method = HttpMethod.Get, Template = PersonRoute }, Reply.With(expected));
        SampleCheck.Equal(expected, await owned.ReadAsync());
        using IOwnedClientApi defaults = RestService.For<IOwnedClientApi>(BaseUrl);
        using IOwnedClientApi selected = (IOwnedClientApi)RestService.For(OwnedClientInterface, BaseUrl, ownedSettings);
        host.Http.Add(new() { Method = HttpMethod.Get, Template = PersonRoute }, Reply.With(expected));
        SampleCheck.Equal(expected, await selected.ReadAsync());
        using IOwnedClientApi selectedDefaults = (IOwnedClientApi)RestService.For(OwnedClientInterface, BaseUrl);

        IRequestBuilder<IRequestLookupApi> lookup = RequestBuilder.ForType<IRequestLookupApi>(host.Settings);
        Func<HttpClient, object[], object?> invoke = lookup.BuildRestResultFuncForMethod(nameof(IRequestLookupApi.BuildAsync), [typeof(int)]);
        using HttpRequestMessage request = await (Task<HttpRequestMessage>)invoke(host.Client, [1])!;
        Console.WriteLine(request.RequestUri); // /lookup/1
        SampleCheck.Equal(host.Settings, lookup.Settings);
        SampleCheck.Equal("/lookup/1", request.RequestUri?.ToString());

        Func<HttpClient, object[], object?> generic = lookup.BuildRestResultFuncForMethod(nameof(IRequestLookupApi.GenericAsync), [typeof(int)], [typeof(int)]);
        using HttpRequestMessage genericRequest = await (Task<HttpRequestMessage>)generic(host.Client, [1])!;
        _ = genericRequest.Options.TryGetValue(new("value"), out int value);
        SampleCheck.Equal(1, value);
        _ = RequestBuilder.ForType<IRequestLookupApi>();
        _ = RequestBuilder.ForType(LookupInterface);
        IRequestBuilder runtimeLookup = RequestBuilder.ForType(LookupInterface, host.Settings);
        Func<HttpClient, object[], object?> text = runtimeLookup.BuildRestResultFuncForMethod(nameof(IRequestLookupApi.BuildAsync), [typeof(string)]);
        using HttpRequestMessage textRequest = await (Task<HttpRequestMessage>)text(host.Client, ["Ada"])!;
        SampleCheck.Equal(host.Settings, runtimeLookup.Settings);
        SampleCheck.Equal("/lookup/Ada", textRequest.RequestUri?.ToString());
        CheckAmbiguousLookup(runtimeLookup);

        CheckRegistrations(host.Settings);
        await CheckResolvedRegistrationsAsync(host.Settings, expected);
        await host.Http.VerifyAllCalledAsync();
    }

    /// <summary>Registers an open generic adapter for a reflected deferred call.</summary>
    /// <param name="host">The local transport and generated JSON serializer.</param>
    /// <returns>Completion after exactly one deferred request is sent.</returns>
    [RequiresUnreferencedCode("The reflected adapter path inspects interface and adapter metadata.")]
    [RequiresDynamicCode("The reflected adapter path closes generic adapter types.")]
    private static async Task CheckReturnAdapterAsync(SampleHost host)
    {
        Person expected = new(1, "Ada");
        host.Http.Add(Route.Get("/content/person"), Reply.With(expected));
        RefitSettings settings = new(host.Settings.ContentSerializer);
        settings.ReturnTypeAdapters.Add(typeof(Content.PersonCallAdapter<>));
        Content.IAdapterApi api = RestService.For<Content.IAdapterApi>(host.Client, settings);
        int before = host.Http.Requests.Count;
        Content.PersonCall<Person> pending = api.Read();
        SampleCheck.Equal(before, host.Http.Requests.Count);
        SampleCheck.Equal(expected, await pending.InvokeAsync(CancellationToken.None));
        SampleCheck.Equal(before + 1, host.Http.Requests.Count);
    }

    /// <summary>Checks that omitted parameter types cannot select an overloaded HTTP method.</summary>
    /// <param name="builder">The runtime-selected builder with numeric and text overloads.</param>
    [RequiresUnreferencedCode("These examples explicitly exercise the opt-in reflection request builder.")]
    [RequiresDynamicCode("These examples explicitly close reflected generic methods.")]
    private static void CheckAmbiguousLookup(IRequestBuilder builder)
    {
        bool rejected = false;
        try
        {
            _ = builder.BuildRestResultFuncForMethod(nameof(IRequestLookupApi.BuildAsync));
        }
        catch (ArgumentException)
        {
            rejected = true;
        }

        SampleCheck.Equal(true, rejected);
    }

    /// <summary>Resolves and sends through generic and runtime-selected registrations on both receivers.</summary>
    /// <param name="settings">The generated JSON settings supplied to registrations.</param>
    /// <param name="expected">The reply matched by each local transport.</param>
    /// <returns>Completion after ordinary and keyed reflected clients send their requests.</returns>
    [RequiresUnreferencedCode("These examples explicitly exercise the opt-in reflection request builder.")]
    [RequiresDynamicCode("These examples explicitly close reflected generic DI types.")]
    private static async Task CheckResolvedRegistrationsAsync(RefitSettings settings, Person expected)
    {
        using SampleHost ordinary = new();
        using SampleHost keyed = new();
        using SampleHost existing = new();
        using SampleHost existingKeyed = new();
        SampleHost[] hosts = [ordinary, keyed, existing, existingKeyed];
        foreach (SampleHost host in hosts)
        {
            host.Http.Add(new() { Method = HttpMethod.Get, Template = PersonRoute }, Reply.With(expected));
        }

        ServiceCollection services = new();
        _ = services.AddSingleton(settings);
        _ = services.AddRefitClient<IClientApi>(static provider => provider.GetRequiredService<RefitSettings>(), "ordinary-verified")
            .ConfigureHttpClient(static client => client.BaseAddress = new(BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => ordinary.Http);
        _ = services.AddKeyedRefitClient(ClientInterface, ServiceKey, static provider => provider.GetRequiredService<RefitSettings>(), "keyed-verified")
            .ConfigureHttpClient(static client => client.BaseAddress = new(BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => keyed.Http);
        ServiceCollection existingServices = new();
        _ = existingServices.AddHttpClient("existing-verified")
            .ConfigureHttpClient(static client => client.BaseAddress = new(BaseUrl))
            .AddRefitClient(ClientInterface, settings)
            .ConfigurePrimaryHttpMessageHandler(() => existing.Http);
        _ = existingServices.AddHttpClient("existing-keyed-verified")
            .ConfigureHttpClient(static client => client.BaseAddress = new(BaseUrl))
            .AddKeyedRefitClient<IClientApi>(ServiceKey, settings)
            .ConfigurePrimaryHttpMessageHandler(() => existingKeyed.Http);

        await using ServiceProvider provider = services.BuildServiceProvider();
        await using ServiceProvider existingProvider = existingServices.BuildServiceProvider();
        SampleCheck.Equal(expected, await provider.GetRequiredService<IClientApi>().ReadAsync());
        SampleCheck.Equal(expected, await ((IClientApi)provider.GetRequiredKeyedService(ClientInterface, ServiceKey)).ReadAsync());
        SampleCheck.Equal(expected, await ((IClientApi)existingProvider.GetRequiredService(ClientInterface)).ReadAsync());
        SampleCheck.Equal(expected, await existingProvider.GetRequiredKeyedService<IClientApi>(ServiceKey).ReadAsync());
        foreach (SampleHost host in hosts)
        {
            await host.Http.VerifyAllCalledAsync();
        }
    }

    /// <summary>Compiles every reflection registration shape on both extension receivers.</summary>
    /// <param name="settings">The generated JSON settings supplied to registrations.</param>
    [RequiresUnreferencedCode("These examples explicitly exercise the opt-in reflection request builder.")]
    [RequiresDynamicCode("These examples explicitly close reflected generic DI types.")]
    private static void CheckRegistrations(RefitSettings settings)
    {
        ServiceCollection services = new();
        _ = services.AddRefitClient<IClientApi>(settings)
            .ConfigureHttpClient(static client => client.BaseAddress = new(BaseUrl));
        _ = services.AddRefitClient(ClientInterface, static _ => null, "runtime-selected");
        _ = services.AddKeyedRefitClient<IClientApi>(ServiceKey, settings);
        _ = services.AddRefitClient<IClientApi>();
        _ = services.AddRefitClient<IClientApi>(settings, "typed-name");
        _ = services.AddRefitClient<IClientApi>(static _ => null);
        _ = services.AddRefitClient<IClientApi>(static _ => null, "typed-factory");
        _ = services.AddRefitClient(ClientInterface);
        _ = services.AddRefitClient(ClientInterface, settings);
        _ = services.AddRefitClient(ClientInterface, settings, "runtime-name");
        _ = services.AddRefitClient(ClientInterface, static _ => null);
        _ = services.AddKeyedRefitClient<IClientApi>(ServiceKey);
        _ = services.AddKeyedRefitClient<IClientApi>(ServiceKey, settings, "keyed-name");
        _ = services.AddKeyedRefitClient<IClientApi>(ServiceKey, static _ => null);
        _ = services.AddKeyedRefitClient<IClientApi>(ServiceKey, static _ => null, "keyed-factory");
        _ = services.AddKeyedRefitClient(ClientInterface, ServiceKey);
        _ = services.AddKeyedRefitClient(ClientInterface, ServiceKey, settings);
        _ = services.AddKeyedRefitClient(ClientInterface, ServiceKey, settings, "runtime-keyed");
        _ = services.AddKeyedRefitClient(ClientInterface, ServiceKey, static _ => null);
        _ = services.AddKeyedRefitClient(ClientInterface, ServiceKey, static _ => null, "runtime-keyed-factory");

        IHttpClientBuilder http = services.AddHttpClient("existing")
            .ConfigureHttpClient(static client => client.BaseAddress = new(BaseUrl));
        _ = http.AddRefitClient<IClientApi>(settings);
        _ = http.AddKeyedRefitClient(ClientInterface, ServiceKey, settings);
        _ = http.AddRefitClient<IClientApi>();
        _ = http.AddRefitClient<IClientApi>(static _ => null);
        _ = http.AddRefitClient(ClientInterface);
        _ = http.AddRefitClient(ClientInterface, settings);
        _ = http.AddRefitClient(ClientInterface, static _ => null);
        _ = http.AddKeyedRefitClient<IClientApi>(ServiceKey);
        _ = http.AddKeyedRefitClient<IClientApi>(ServiceKey, settings);
        _ = http.AddKeyedRefitClient<IClientApi>(ServiceKey, static _ => null);
        _ = http.AddKeyedRefitClient(ClientInterface, ServiceKey);
        _ = http.AddKeyedRefitClient(ClientInterface, ServiceKey, static _ => null);
    }
}
