// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Checks generated creation, settings and factory registration without a live service.</summary>
internal static class Clients
{
    /// <summary>The base address used by local transports.</summary>
    private const string BaseUrl = "https://people.example";

    /// <summary>The factory key used to distinguish one interface registration.</summary>
    private const string ServiceKey = "regional";

    /// <summary>The handler token used to distinguish it from the explicit caller token.</summary>
    private const string HandlerToken = "sample-handler-token";

    /// <summary>The explicit token supplied by the caller.</summary>
    private const string CallerToken = "sample-caller-token";

    /// <summary>The header name compared by authorization reproductions.</summary>
    private const string AuthorizationHeaderName = "Authorization";

    /// <summary>The expected getter invocation count for a generated authorized call with a handler.</summary>
    private const int DoubleInvocationCount = 2;

    /// <summary>The local person route shared by client transports.</summary>
    private const string PersonRoute = "/clients/person";

    /// <summary>The local route used to verify explicit token replacement.</summary>
    private const string ExplicitRoute = "/clients/explicit";

    /// <summary>The CLR key used to demonstrate naming conventions.</summary>
    private const string NamingKey = "PageSize";

    /// <summary>The expected camel-case spelling shared by JSON and query keys.</summary>
    private const string CamelKey = "pageSize";

    /// <summary>The page size written by each naming example.</summary>
    private const int NamingPageSize = 5;

    /// <summary>The interface selected by the runtime-Type creation example.</summary>
    private static readonly Type ClientInterface = typeof(IClientApi);

    /// <summary>The disposable interface selected by the URL creation example.</summary>
    private static readonly Type OwnedClientInterface = typeof(IOwnedClientApi);

    /// <summary>The interface supplied to custom implementation registrations.</summary>
    private static readonly Type RegisteredInterface = typeof(IRegisteredClient);

    /// <summary>The serializer options reused by the generated clients.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(SampleJsonContext.Default.Options) { TypeInfoResolver = SampleJsonContext.Default };

    /// <summary>The settings reused by clients with no additional options.</summary>
    private static readonly RefitSettings JsonSettings = new(new SystemTextJsonContentSerializer(JsonOptions));

    /// <summary>Runs the generated examples; reflection examples have a separate entry point.</summary>
    /// <param name="host">The shared local transport used by the documentation runner.</param>
    /// <returns>A task that completes after all demonstrated calls are checked.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        await SettingsPolicies.RunAsync(JsonSettings.ContentSerializer);
        Person expected = new(1, "Ada");
        host.Http.Add(new() { Method = HttpMethod.Get, Template = PersonRoute }, Reply.With(expected));

        IClientApi api = RestService.ForGenerated<IClientApi>(host.Client, SampleJsonContext.Default);
        Person person = await api.ReadAsync();
        Console.WriteLine(person.Name); // Ada
        SampleCheck.Equal(expected, person);

        host.Http.Add(new() { Method = HttpMethod.Get, Template = PersonRoute }, Reply.With(expected));
        IClientApi withSettings = RestService.ForGenerated<IClientApi>(host.Client, JsonSettings);
        SampleCheck.Equal(expected, await withSettings.ReadAsync());

        host.Http.Add(new() { Method = HttpMethod.Get, Template = PersonRoute }, Reply.With(expected));
        RefitSettings shortcut = RefitSettings.ForJsonContext(SampleJsonContext.Default);
        IClientApi withShortcut = RestService.ForGenerated<IClientApi>(host.Client, shortcut);
        SampleCheck.Equal(expected, await withShortcut.ReadAsync());

        host.Http.Add(new() { Method = HttpMethod.Get, Template = PersonRoute }, Reply.With(expected));
        IClientApi bridged = RestService.ForGenerated<IClientApi>(host.Client, SampleJsonContext.Default, new RefitSettings { Buffered = true });
        SampleCheck.Equal(expected, await bridged.ReadAsync());

        object implementation = RestService.ForGenerated(ClientInterface, host.Client, JsonSettings);
        IClientApi selected = (IClientApi)implementation;
        Console.WriteLine(selected.GetType().Name);
        host.Http.Add(new() { Method = HttpMethod.Get, Template = PersonRoute }, Reply.With(expected));
        SampleCheck.Equal(expected, await selected.ReadAsync());

        _ = RestService.ForGenerated<IClientApi>(host.Client);
        await CheckNamingSettingsAsync();
        await CheckNamingWithContextAsync();
        await CheckOwnedClientAsync(expected);
        CheckRegistrationInfrastructure(host.Client);
        await CheckContextClientsAsync(expected);
        await CheckFactoryClientsAsync(expected);
        await CheckAuthorizationAsync(host, expected);
        await host.Http.VerifyAllCalledAsync();
    }

    /// <summary>Checks the naming shortcuts with one generated context added to each. The settings' naming policy wins.</summary>
    /// <returns>A task that completes after the serialized naming conventions are checked.</returns>
    private static async Task CheckNamingWithContextAsync()
    {
        RefitSettings camel = RefitSettings.CamelCase().UseJsonContext(ClientNamingJsonContext.Default);
        RefitSettings snake = RefitSettings.SnakeCase().UseJsonContext(ClientNamingJsonContext.Default);
        RefitSettings kebab = RefitSettings.KebabCase().UseJsonContext(ClientNamingJsonContext.Default);
        RefitSettings[] naming = [camel, snake, kebab];
        string[] expectedBodies = ["{\"pageSize\":5}", "{\"page_size\":5}", "{\"page-size\":5}"];
        for (int index = 0; index < naming.Length; index++)
        {
            RefitSettings settings = naming[index];
            Console.WriteLine(settings.UrlParameterKeyFormatter.Format(NamingKey));
            using HttpContent content = settings.ContentSerializer.ToHttpContent(new ClientNamingInput(NamingPageSize));
            Console.WriteLine(await content.ReadAsStringAsync());
            SampleCheck.Equal(expectedBodies[index], await content.ReadAsStringAsync());
        }
    }

    /// <summary>Checks every settings factory and constructor shape with generated JSON metadata.</summary>
    /// <returns>A task that completes after the serialized naming conventions are checked.</returns>
    private static async Task CheckNamingSettingsAsync()
    {
        RefitSettings camel = RefitSettings.CamelCase();
        RefitSettings snake = RefitSettings.SnakeCase();
        RefitSettings kebab = RefitSettings.KebabCase();
        RefitSettings[] naming = [camel, snake, kebab];
        string[] expectedKeys = [CamelKey, "page_size", "page-size"];
        string[] expectedBodies = ["{\"pageSize\":5}", "{\"page_size\":5}", "{\"page-size\":5}"];
        System.Text.Json.Serialization.JsonSerializerContext[] contexts = [ClientCamelJsonContext.Default, ClientSnakeJsonContext.Default, ClientKebabJsonContext.Default];
        for (int index = 0; index < naming.Length; index++)
        {
            RefitSettings settings = naming[index];
            SystemTextJsonContentSerializer original = (SystemTextJsonContentSerializer)settings.ContentSerializer;
            SampleCheck.Equal(expectedKeys[index], original.SerializerOptions.PropertyNamingPolicy!.ConvertName(NamingKey));
            System.Text.Json.Serialization.JsonSerializerContext context = contexts[index];
            JsonSerializerOptions options = new(context.Options) { TypeInfoResolver = context };
            settings.ContentSerializer = new SystemTextJsonContentSerializer(options);
            Console.WriteLine(settings.UrlParameterKeyFormatter.Format(NamingKey));
            using HttpContent content = settings.ContentSerializer.ToHttpContent(new ClientNamingInput(NamingPageSize));
            Console.WriteLine(await content.ReadAsStringAsync());
            SampleCheck.Equal(expectedBodies[index], await content.ReadAsStringAsync());
        }

        SampleCheck.Equal(CamelKey, camel.UrlParameterKeyFormatter.Format(NamingKey));
        SampleCheck.Equal("page_size", snake.UrlParameterKeyFormatter.Format(NamingKey));
        SampleCheck.Equal("page-size", kebab.UrlParameterKeyFormatter.Format(NamingKey));

        RefitSettings defaults = new() { ContentSerializer = JsonSettings.ContentSerializer };
        RefitSettings serializerOnly = new(JsonSettings.ContentSerializer);
        RefitSettings values = new(JsonSettings.ContentSerializer, new DefaultUrlParameterFormatter());
        RefitSettings forms = new(JsonSettings.ContentSerializer, null, new DefaultFormUrlEncodedParameterFormatter());
        RefitSettings keys = new(JsonSettings.ContentSerializer, null, null, new CamelCaseUrlParameterKeyFormatter());
        SampleCheck.Equal(serializerOnly.ContentSerializer, defaults.ContentSerializer);
        SampleCheck.Equal(typeof(DefaultUrlParameterFormatter), values.UrlParameterFormatter.GetType());
        SampleCheck.Equal(typeof(DefaultFormUrlEncodedParameterFormatter), forms.FormUrlEncodedParameterFormatter.GetType());
        SampleCheck.Equal(CamelKey, keys.UrlParameterKeyFormatter.Format(NamingKey));
    }

    /// <summary>Checks URL creation using a locally supplied handler.</summary>
    /// <param name="expected">The reply returned by the transport.</param>
    /// <returns>A task that completes after the generated client sends its request.</returns>
    private static async Task CheckOwnedClientAsync(Person expected)
    {
        StubHttp local = new();
        local.Add(new() { Method = HttpMethod.Get, Template = PersonRoute }, Reply.With(expected));
        RefitSettings settings = new(JsonSettings.ContentSerializer) { HttpMessageHandlerFactory = () => local };
        _ = local.ToSettings(settings);

        using HttpClient client = RestService.CreateHttpClient(BaseUrl, settings);
        IClientApi api = RestService.ForGenerated<IClientApi>(client, settings);
        Person person = await api.ReadAsync();
        SampleCheck.Equal(expected, person);
        _ = RestService.ForGenerated(ClientInterface, client, settings);

        using IOwnedClientApi owned = RestService.ForGenerated<IOwnedClientApi>(BaseUrl, settings);
        local.Add(new() { Method = HttpMethod.Get, Template = PersonRoute }, Reply.With(expected));
        SampleCheck.Equal(expected, await owned.ReadAsync());
        using IOwnedClientApi contextOwned = RestService.ForGenerated<IOwnedClientApi>(BaseUrl, SampleJsonContext.Default);
        using IOwnedClientApi defaultOwned = RestService.ForGenerated<IOwnedClientApi>(BaseUrl);
        using IOwnedClientApi selectedOwned = (IOwnedClientApi)RestService.ForGenerated(OwnedClientInterface, BaseUrl, settings);
        local.Add(new() { Method = HttpMethod.Get, Template = PersonRoute }, Reply.With(expected));
        SampleCheck.Equal(expected, await selectedOwned.ReadAsync());
        await local.VerifyAllCalledAsync();
    }

    /// <summary>Checks public factory registration using a small hand-written implementation.</summary>
    /// <param name="client">The HTTP client retained by the registered implementation.</param>
    private static void CheckRegistrationInfrastructure(HttpClient client)
    {
        RestService.RegisterGeneratedFactory<IRegisteredClient>(static (http, builder) => new RegisteredClient(http, builder.Settings));
        IRegisteredClient typed = RestService.ForGenerated<IRegisteredClient>(client, JsonSettings);

        RestService.RegisterGeneratedFactory(RegisteredInterface, static (http, builder) => new RegisteredClient(http, builder.Settings));
        IRegisteredClient selected = (IRegisteredClient)RestService.ForGenerated(RegisteredInterface, client, JsonSettings);
        RestService.RegisterGeneratedSettingsFactory<IRegisteredClient>(static (http, settings) => new RegisteredClient(http, settings));
        IRegisteredClient inline = RestService.ForGenerated<IRegisteredClient>(client, JsonSettings);
        SampleCheck.Equal(JsonSettings, typed.Settings);
        SampleCheck.Equal(client.BaseAddress, selected.BaseAddress);
        SampleCheck.Equal(JsonSettings, inline.Settings);
    }

    /// <summary>Checks ordinary, keyed and settings-factory registrations that take a JSON context.</summary>
    /// <param name="expected">The reply returned by the transport.</param>
    /// <returns>A task that completes after registered clients have sent their requests.</returns>
    private static async Task CheckContextClientsAsync(Person expected)
    {
        ServiceCollection services = new();
        _ = services.AddRefitGeneratedClient<IClientApi>(SampleJsonContext.Default)
            .ConfigureHttpClient(static client => client.BaseAddress = new(BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => CreateTransport(expected));
        await using ServiceProvider provider = services.BuildServiceProvider();
        IClientApi api = provider.GetRequiredService<IClientApi>();
        Person person = await api.ReadAsync();
        Console.WriteLine(person.Name); // Ada
        SampleCheck.Equal(expected, person);

        ServiceCollection keyedServices = new();
        _ = keyedServices.AddKeyedRefitGeneratedClient<IClientApi>(ServiceKey, SampleJsonContext.Default)
            .ConfigureHttpClient(static client => client.BaseAddress = new(BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => CreateTransport(expected));
        await using ServiceProvider keyedProvider = keyedServices.BuildServiceProvider();
        IClientApi regional = keyedProvider.GetRequiredKeyedService<IClientApi>(ServiceKey);
        SampleCheck.Equal(expected, await regional.ReadAsync());

        ServiceCollection contextFactoryServices = new();
        _ = contextFactoryServices.AddSingleton(RefitSettings.SnakeCase());
        _ = contextFactoryServices.AddRefitGeneratedClient<IClientApi>(SampleJsonContext.Default, static serviceProvider => serviceProvider.GetRequiredService<RefitSettings>(), "snake-people")
            .ConfigureHttpClient(static client => client.BaseAddress = new(BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => CreateTransport(expected));
        await using ServiceProvider contextFactoryProvider = contextFactoryServices.BuildServiceProvider();
        SampleCheck.Equal(expected, await contextFactoryProvider.GetRequiredService<IClientApi>().ReadAsync());
        SampleCheck.Equal(contextFactoryProvider.GetRequiredService<RefitSettings>(), contextFactoryProvider.GetRequiredService<SettingsFor<IClientApi>>().Settings);
    }

    /// <summary>Checks ordinary, keyed and scoped-token generated registrations.</summary>
    /// <param name="expected">The reply returned by the transport.</param>
    /// <returns>A task that completes after registered clients have sent their requests.</returns>
    private static async Task CheckFactoryClientsAsync(Person expected)
    {
        ServiceCollection settingsServices = new();
        _ = settingsServices.AddSingleton<ISettingsFor>(new SettingsFor<IClientApi>(JsonSettings));
        _ = settingsServices.AddRefitGeneratedClient<IClientApi>(JsonSettings)
            .ConfigureHttpClient(static client => client.BaseAddress = new(BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => CreateTransport(expected));
        await using ServiceProvider settingsProvider = settingsServices.BuildServiceProvider();
        IClientApi settingsApi = settingsProvider.GetRequiredService<IClientApi>();
        Person fromSettings = await settingsApi.ReadAsync();
        SampleCheck.Equal(expected, fromSettings);

        ServiceCollection keyedSettingsServices = new();
        _ = keyedSettingsServices.AddKeyedRefitGeneratedClient<IClientApi>(ServiceKey, JsonSettings, "regional-people")
            .ConfigureHttpClient(static client => client.BaseAddress = new(BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => CreateTransport(expected));
        await using ServiceProvider keyedSettingsProvider = keyedSettingsServices.BuildServiceProvider();
        IClientApi regionalWithSettings = keyedSettingsProvider.GetRequiredKeyedService<IClientApi>(ServiceKey);
        SampleCheck.Equal(expected, await regionalWithSettings.ReadAsync());

        SettingsFor<IClientApi> holder = settingsProvider.GetRequiredService<SettingsFor<IClientApi>>();
        ISettingsFor untypedHolder = settingsProvider.GetRequiredService<ISettingsFor>();
        SampleCheck.Equal(JsonSettings, holder.Settings);
        SampleCheck.Equal(JsonSettings, untypedHolder.Settings);

        ServiceCollection factoryServices = new();
        _ = factoryServices.AddSingleton(JsonSettings);
        _ = factoryServices.AddRefitGeneratedClient<IClientApi>(static serviceProvider => serviceProvider.GetRequiredService<RefitSettings>(), "people");
        _ = factoryServices.AddRefitGeneratedClient<IClientApi>();
        _ = factoryServices.AddRefitGeneratedClient<IClientApi>(JsonSettings, "named-people");
        _ = factoryServices.AddRefitGeneratedClient<IClientApi>(static _ => JsonSettings);
        _ = factoryServices.AddKeyedRefitGeneratedClient<IClientApi>(ServiceKey);
        _ = factoryServices.AddKeyedRefitGeneratedClient<IClientApi>(ServiceKey, JsonSettings);
        _ = factoryServices.AddKeyedRefitGeneratedClient<IClientApi>(ServiceKey, static _ => JsonSettings);
        _ = factoryServices.AddKeyedRefitGeneratedClient<IClientApi>(ServiceKey, static _ => JsonSettings, "keyed-people");
        _ = factoryServices.AddRefitGeneratedClient<IClientApi>(static serviceProvider => serviceProvider.GetRequiredService<RefitSettings>(), "factory-checked")
            .ConfigureHttpClient(static client => client.BaseAddress = new(BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => CreateTransport(expected));
        _ = factoryServices.AddKeyedRefitGeneratedClient<IClientApi>(ServiceKey, static serviceProvider => serviceProvider.GetRequiredService<RefitSettings>(), "keyed-factory-checked")
            .ConfigureHttpClient(static client => client.BaseAddress = new(BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => CreateTransport(expected));
        await using ServiceProvider factoryProvider = factoryServices.BuildServiceProvider();
        SampleCheck.Equal(expected, await factoryProvider.GetRequiredService<IClientApi>().ReadAsync());
        SampleCheck.Equal(expected, await factoryProvider.GetRequiredKeyedService<IClientApi>(ServiceKey).ReadAsync());
        SampleCheck.Equal(JsonSettings, factoryProvider.GetRequiredService<SettingsFor<IClientApi>>().Settings);

        ServiceCollection tokenServices = new();
        StubHttp tokenTransport = CreateTransport(expected);
        tokenTransport.Add(new() { Method = HttpMethod.Get, Template = ExplicitRoute, Headers = [(AuthorizationHeaderName, $"Bearer {HandlerToken}")] }, Reply.With(expected));
        _ = tokenServices.AddRefitGeneratedClient<IClientApi>(JsonSettings)
            .ConfigureHttpClient(static client => client.BaseAddress = new(BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => tokenTransport)
            .AddAuthorizationHeaderValueProvider(static (_, _, _) => ValueTask.FromResult(HandlerToken));
        await using ServiceProvider tokenProvider = tokenServices.BuildServiceProvider();
        IClientApi secured = tokenProvider.GetRequiredService<IClientApi>();
        SampleCheck.Equal(expected, await secured.ReadAsync());
        SampleCheck.Equal($"Bearer {HandlerToken}", tokenTransport.Requests[0].Headers.Authorization?.ToString());
        SampleCheck.Equal(expected, await secured.ExplicitAsync(CallerToken));
        await tokenTransport.VerifyAllCalledAsync();
        await CheckEmptyAuthorizationAsync(expected);
    }

    /// <summary>Checks that an empty scoped token removes both default and explicit authorization.</summary>
    /// <param name="expected">The reply returned by the local transport.</param>
    /// <returns>Completion after requests with no authorization header are matched.</returns>
    private static async Task CheckEmptyAuthorizationAsync(Person expected)
    {
        StubHttp transport = CreateTransport(expected);
        transport.Add(new() { Method = HttpMethod.Get, Template = ExplicitRoute, Where = static request => request.Headers.Authorization is null }, Reply.With(expected));
        ServiceCollection services = new();
        _ = services.AddRefitGeneratedClient<IClientApi>(JsonSettings)
            .ConfigureHttpClient(static client => client.BaseAddress = new(BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => transport)
            .AddAuthorizationHeaderValueProvider(static (_, _, _) => ValueTask.FromResult(string.Empty));
        await using ServiceProvider provider = services.BuildServiceProvider();
        IClientApi api = provider.GetRequiredService<IClientApi>();
        SampleCheck.Equal(expected, await api.ReadAsync());
        SampleCheck.Equal(true, transport.Requests[0].Headers.Authorization is null);
        SampleCheck.Equal(expected, await api.ExplicitAsync(CallerToken));
        await transport.VerifyAllCalledAsync();
    }

    /// <summary>Builds a handler that returns a generated-JSON reply.</summary>
    /// <param name="expected">The reply returned by the transport.</param>
    /// <returns>A fresh handler that can be owned by one factory pipeline.</returns>
    private static StubHttp CreateTransport(Person expected)
    {
        StubHttp http = new();
        _ = http.ToSettings(JsonSettings);
        http.Add(new() { Method = HttpMethod.Get, Template = PersonRoute }, Reply.With(expected));
        return http;
    }

    /// <summary>Checks the handler behavior that differs from generated request preparation.</summary>
    /// <param name="host">The shared plain transport used as the control.</param>
    /// <param name="expected">The reply returned by the transport.</param>
    /// <returns>A task that completes after token replacement and invocation counts are checked.</returns>
    private static async Task CheckAuthorizationAsync(SampleHost host, Person expected)
    {
        StubHttp local = new();
        int calls = 0;
        RefitSettings settings = new(JsonSettings.ContentSerializer)
        {
            HttpMessageHandlerFactory = () => local,
            AuthorizationHeaderValueGetter = (_, _) =>
            {
                calls++;
                return ValueTask.FromResult(HandlerToken);
            },
        };
        _ = local.ToSettings(settings);
        local.Add(new() { Method = HttpMethod.Get, Template = "/clients/authorized", Headers = [(AuthorizationHeaderName, $"Bearer {HandlerToken}")] }, Reply.With(expected));
        local.Add(new() { Method = HttpMethod.Get, Template = ExplicitRoute, Headers = [(AuthorizationHeaderName, $"Bearer {HandlerToken}")] }, Reply.With(expected));

        using HttpClient client = RestService.CreateHttpClient(BaseUrl, settings);
        IClientApi api = RestService.ForGenerated<IClientApi>(client, settings);
        await api.AuthorizedAsync();
        Console.WriteLine(calls); // 2
        SampleCheck.Equal(DoubleInvocationCount, calls);

        calls = 0;

        await api.ExplicitAsync(CallerToken);
        Console.WriteLine(calls); // 1
        SampleCheck.Equal(1, calls);
        await local.VerifyAllCalledAsync();
        host.Http.Add(new() { Method = HttpMethod.Get, Template = "/clients/authorized", Headers = [(AuthorizationHeaderName, $"Bearer {HandlerToken}")] }, Reply.With(expected));
        host.Http.Add(new() { Method = HttpMethod.Get, Template = ExplicitRoute, Headers = [(AuthorizationHeaderName, $"Bearer {CallerToken}")] }, Reply.With(expected));
        IClientApi plainApi = RestService.ForGenerated<IClientApi>(host.Client, settings);
        calls = 0;
        await plainApi.AuthorizedAsync();
        SampleCheck.Equal(1, calls);
        calls = 0;
        await plainApi.ExplicitAsync(CallerToken);
        SampleCheck.Equal(0, calls);
        await host.Http.VerifyAllCalledAsync();
    }
}
