// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Checks generated metadata, synchronous and streaming serialization and missing-type failures.</summary>
internal static class Serialization
{
    /// <summary>The camelCase reply the local people service sends.</summary>
    private const string PersonJson = "{\"id\":1,\"name\":\"Ada\"}";

    /// <summary>Resolves only models registered with the shared generated JSON context.</summary>
    private static readonly JsonSerializerOptions Options = new(SampleJsonContext.Default.Options) { TypeInfoResolver = SampleJsonContext.Default, };

    /// <summary>Reuses immutable generated-metadata serializer configuration across the demonstrations.</summary>
    private static readonly SystemTextJsonContentSerializer Serializer = new(Options);

    /// <summary>Builds a serializer straight from the shared generated JSON context, which never falls back to reflection.</summary>
    private static readonly SystemTextJsonContentSerializer ContextSerializer = SystemTextJsonContentSerializer.ForContext(SampleJsonContext.Default);

    /// <summary>Checks person serialization paths, combined contexts and rejection of missing metadata.</summary>
    /// <returns>A task that completes after the sample assertions pass.</returns>
    internal static async Task RunAsync()
    {
        SystemTextJsonContentSerializer serializer = Serializer;
        using HttpContent content = serializer.ToHttpContent(new Person(1, "Ada"));
        Person? person = await serializer.FromHttpContentAsync<Person>(content);
        Console.WriteLine(person?.Name); // Ada

        SampleCheck.Equal("Ada", person?.Name);
        SampleCheck.Equal(ContentSerializerNameStyle.SystemTextJson, serializer.NameStyle);

        JsonTypeInfo<Person> personInfo = SampleJsonContext.Default.Person;
        string json = JsonSerializer.Serialize(new(1, "Ada"), personInfo);
        Person? restored = JsonSerializer.Deserialize(json, personInfo);
        Console.WriteLine(restored?.Name); // Ada

        SampleCheck.Equal("Ada", restored?.Name);
        SampleCheck.Equal(Options, Serializer.SerializerOptions);
        await RoundTripTypedBodiesAsync(new(1, "Ada"), personInfo, CancellationToken.None);
        await RunContextSerializerAsync();
        await RunContextClientAsync();
        await RunDefaultsAsync();
        await RunFastPathAsync();
        await RunCombinedContextsAsync();
        VerifyMissingMetadata();
    }

    /// <summary>Reads and writes buffered JSON, streamed JSON and newline-delimited JSON with a per-call contract.</summary>
    /// <param name="person">The model sent in each representation.</param>
    /// <param name="contract">The generated contract used for each read and write.</param>
    /// <param name="token">Cancels body reads and stream enumeration.</param>
    /// <returns>Completion after checking each representation.</returns>
    private static async Task RoundTripTypedBodiesAsync(Person person, JsonTypeInfo<Person> contract, CancellationToken token)
    {
        SystemTextJsonContentSerializer serializer = SystemTextJsonContentSerializer.ForContext(PageJsonContext.Default, allowReflectionFallback: false);
        using HttpContent content = serializer.ToHttpContent(person, contract);
        SampleCheck.Equal(person, await serializer.FromHttpContentAsync(content, contract, token));

        using HttpContent buffered = serializer.ToHttpContentSynchronous(person, contract);
        string json = await buffered.ReadAsStringAsync(token);
        SampleCheck.Equal(person, serializer.DeserializeFromString(json, contract));

        using HttpContent streamed = serializer.ToStreamingHttpContent(person, contract);
        SampleCheck.Equal(json, await streamed.ReadAsStringAsync(token));
        using StringContent lines = new($"{json}\n{json}\n");
        await using Stream source = await lines.ReadAsStreamAsync(token);
        int count = 0;
        await foreach (Person? item in serializer.DeserializeStreamAsync(source, StreamingContentFormat.JsonLines, contract, token))
        {
            SampleCheck.Equal(person, item);
            count++;
        }

        SampleCheck.Equal(2, count);
    }

    /// <summary>Checks whether a configured serializer can buffer, stream and parse a person.</summary>
    /// <param name="serializer">The serializer used by the client.</param>
    /// <param name="token">Cancels reading the HTTP content.</param>
    /// <returns>Completion after checking both representations.</returns>
    private static async Task CheckContentFormatsAsync(IHttpContentSerializer serializer, CancellationToken token)
    {
        if (serializer is not ISynchronousContentSerializer writer || serializer is not ISynchronousContentDeserializer reader)
        {
            throw new InvalidOperationException("This example needs synchronous serialization and deserialization.");
        }

        Person person = new(1, "Ada");
        using HttpContent buffered = writer.ToHttpContentSynchronous(person);
        using HttpContent streamed = writer.ToStreamingHttpContent(person);
        SampleCheck.Equal(person, reader.DeserializeFromString<Person>(await buffered.ReadAsStringAsync(token)));
        SampleCheck.Equal(person, await serializer.FromHttpContentAsync<Person>(streamed, token));
    }

    /// <summary>Round-trips a person through a serializer built straight from the JSON context.</summary>
    /// <returns>A task that completes after the round trip is checked.</returns>
    private static async Task RunContextSerializerAsync()
    {
        using HttpContent content = ContextSerializer.ToHttpContent(new Person(1, "Ada"));
        Person? person = await ContextSerializer.FromHttpContentAsync<Person>(content);
        Console.WriteLine(person?.Name); // Ada

        SampleCheck.Equal("Ada", person?.Name);
        SampleCheck.Equal(SampleJsonContext.Default.Options, ContextSerializer.SerializerOptions);
    }

    /// <summary>Creates a generated client from the JSON context alone, then from settings, and reads a person through each.</summary>
    /// <returns>A task that completes after both replies are checked.</returns>
    private static async Task RunContextClientAsync()
    {
        using StubHttp http = new() { { Route.Get("/people/{id}"), Reply.Json(PersonJson) }, { Route.Get("/people/{id}"), Reply.Json(PersonJson) } };
        using HttpClient client = CreateClient(http);

        IPeopleApi api = RestService.ForGenerated<IPeopleApi>(client, SampleJsonContext.Default);
        Person person = await api.GetPersonAsync(1, CancellationToken.None);
        Console.WriteLine(person.Name); // Ada

        SampleCheck.Equal(new(1, "Ada"), person);

        RefitSettings settings = new(Serializer);
        await CheckContentFormatsAsync(settings.ContentSerializer, CancellationToken.None);
        IPeopleApi withSettings = RestService.ForGenerated<IPeopleApi>(client, settings);
        Person fromSettings = await withSettings.GetPersonAsync(1, CancellationToken.None);
        Console.WriteLine(fromSettings.Name); // Ada

        SampleCheck.Equal(person, fromSettings);
        await http.VerifyAllCalledAsync();
    }

    /// <summary>Creates a client whose requests go to a local handler.</summary>
    /// <param name="http">The handler that answers every request.</param>
    /// <returns>A client with the people service base address. The caller owns the handler.</returns>
    private static HttpClient CreateClient(StubHttp http) => new(http, disposeHandler: false) { BaseAddress = new("https://people.example") };

    /// <summary>Checks fresh defaults and configuring the default constructor for generated metadata.</summary>
    /// <returns>Completion of the serializer default checks.</returns>
    private static async Task RunDefaultsAsync()
    {
        SystemTextJsonContentSerializer defaults = new();
        JsonSerializerOptions defaultOptions = defaults.SerializerOptions;
        defaultOptions.TypeInfoResolver = SampleJsonContext.Default;
        JsonSerializerOptions separateDefaults = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
        using HttpContent content = defaults.ToHttpContent(new Person(1, "Ada"));
        Person? person = await defaults.FromHttpContentAsync<Person>(content, CancellationToken.None);
        Console.WriteLine(person!.Name); // Ada
        SampleCheck.Equal("Ada", person.Name);
        SampleCheck.Equal(false, ReferenceEquals(defaultOptions, separateDefaults));
        SampleCheck.Equal(true, defaultOptions.PropertyNameCaseInsensitive);
        SampleCheck.Equal(false, defaultOptions.AllowDuplicateProperties);
        SampleCheck.Equal(System.Text.Json.Serialization.JsonNumberHandling.Strict, defaultOptions.NumberHandling);
        SampleCheck.Equal(0, defaultOptions.Converters.Count);
        SampleCheck.Equal("displayName", defaultOptions.PropertyNamingPolicy!.ConvertName("DisplayName"));
    }

    /// <summary>Checks that fast-path options work after a generated resolver is supplied.</summary>
    /// <returns>A task that completes after the sample assertions pass.</returns>
    private static async Task RunFastPathAsync()
    {
        JsonSerializerOptions fastOptions = SystemTextJsonContentSerializer.GetFastPathJsonSerializerOptions();
        fastOptions.TypeInfoResolver = SampleJsonContext.Default;
        SystemTextJsonContentSerializer fastSerializer = new(fastOptions);
        using HttpContent fastContent = fastSerializer.ToHttpContentSynchronous(new Person(1, "Ada"));
        Person? fromFastContent = await fastSerializer.FromHttpContentAsync<Person>(fastContent);
        Console.WriteLine(fromFastContent?.Name); // Ada

        SampleCheck.Equal("Ada", fromFastContent?.Name);
        SampleCheck.Equal(0, fastOptions.Converters.Count);
    }

    /// <summary>Checks that registering a second context enables the separately registered page model.</summary>
    /// <returns>A task that completes after the sample assertions pass.</returns>
    private static async Task RunCombinedContextsAsync()
    {
        RefitSettings settings = RefitSettings.ForJsonContext(SampleJsonContext.Default, allowReflectionFallback: false)
            .UseJsonContext(PageJsonContext.Default, allowReflectionFallback: false);
        SystemTextJsonContentSerializer registeredSerializer = ContextSerializer.WithContext(PageJsonContext.Default);
        SampleCheck.Equal(registeredSerializer, registeredSerializer.WithContext(PageJsonContext.Default, allowReflectionFallback: false));
        using HttpContent registeredContent = registeredSerializer.ToHttpContent(new Page<Person>([new(1, "Ada")]));
        Page<Person>? registeredPage = await settings.ContentSerializer.FromHttpContentAsync<Page<Person>>(registeredContent);
        Console.WriteLine(registeredPage?.Items[0].Name); // Ada

        SampleCheck.Equal("Ada", registeredPage?.Items[0].Name);

        IJsonTypeInfoResolver resolver = JsonTypeInfoResolver.Combine(SampleJsonContext.Default, PageJsonContext.Default);
        JsonSerializerOptions combinedOptions = new(SampleJsonContext.Default.Options) { TypeInfoResolver = resolver };
        SystemTextJsonContentSerializer combinedSerializer = new(combinedOptions);
        using HttpContent pageContent = combinedSerializer.ToHttpContent(new Page<Person>([new(1, "Ada")]));
        Page<Person>? page = await combinedSerializer.FromHttpContentAsync<Page<Person>>(pageContent);
        Console.WriteLine(page?.Items[0].Name); // Ada

        SampleCheck.Equal("Ada", page?.Items[0].Name);
    }

    /// <summary>Checks that the person-only resolver rejects an unregistered closed page type.</summary>
    /// <exception cref="InvalidOperationException">The serializer unexpectedly accepts the unregistered page model.</exception>
    private static void VerifyMissingMetadata()
    {
        try
        {
            // Options resolves only SampleJsonContext's roots. Page<Person> belongs to PageJsonContext.
            using HttpContent missingContent = Serializer.ToHttpContent(new Page<Person>([]));
            throw new InvalidOperationException("Missing JSON metadata should fail.");
        }
        catch (NotSupportedException)
        {
            Console.WriteLine("Register Page<Person> or combine the generated contexts.");
        }
    }
}
