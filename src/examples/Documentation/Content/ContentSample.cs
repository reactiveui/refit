// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using Refit.Testing;

namespace Refit.Documentation.Content;

/// <summary>Checks content APIs using generated JSON metadata.</summary>
internal static class ContentSample
{
    /// <summary>The JSON body used by the stream readers.</summary>
    private const string PersonJson = """{"Id":1,"Name":"Ada"}""";

    /// <summary>The options supplying all types written by the inferred-object converter.</summary>
    private static readonly JsonSerializerOptions Options = new(ContentJsonContext.Default.Options) { TypeInfoResolver = ContentJsonContext.Default };

    /// <summary>Runs the content and adapter demonstrations.</summary>
    /// <returns>Completion of all checked examples.</returns>
    internal static async Task RunAsync()
    {
        SystemTextJsonContentSerializer serializer = new(Options);
        Person person = new(1, "Ada");
        await CheckLinesAsync(serializer, person);
        await CheckStreamingAsync(serializer, person);
        CheckConverter(person);
        await CheckAdapterAsync(person);
    }

    /// <summary>Checks compact JSON Lines output and an empty sequence.</summary>
    /// <param name="serializer">The serializer supplying generated metadata.</param>
    /// <param name="person">The value written twice.</param>
    /// <returns>Completion of the content checks.</returns>
    private static async Task CheckLinesAsync(SystemTextJsonContentSerializer serializer, Person person)
    {
        using JsonLinesContent content = new(new[] { person, person }, serializer);
        string body = await content.ReadAsStringAsync();
        Console.WriteLine(JsonLinesContent.JsonLinesMediaType);
        Console.WriteLine(body);
        SampleCheck.Equal($"{PersonJson}\n{PersonJson}", body);
        SampleCheck.Equal(JsonLinesContent.JsonLinesMediaType, content.Headers.ContentType!.MediaType!);
        using JsonLinesContent empty = new(Array.Empty<Person>(), serializer);
        SampleCheck.Equal(string.Empty, await empty.ReadAsStringAsync());
    }

    /// <summary>Checks all stream formats and caller ownership of direct input streams.</summary>
    /// <typeparam name="TSerializer">The streaming serializer capability selected by the caller.</typeparam>
    /// <param name="streaming">The serializer supplying generated metadata.</param>
    /// <param name="person">The expected item.</param>
    /// <returns>Completion of the stream checks.</returns>
    private static async Task CheckStreamingAsync<TSerializer>(TSerializer streaming, Person person)
        where TSerializer : IStreamingContentSerializer
    {
        (StreamingContentFormat Format, string Body)[] cases =
        [
            (StreamingContentFormat.JsonArray, $"[{PersonJson}]"),
            (StreamingContentFormat.JsonLines, $"  \r\n{PersonJson}\r\n"),
            (StreamingContentFormat.ServerSentEvents, $"data: {PersonJson}\n\n"),
        ];
        foreach ((StreamingContentFormat format, string body) in cases)
        {
            await using MemoryStream stream = new(Encoding.UTF8.GetBytes(body));
            await foreach (Person? item in streaming.DeserializeStreamAsync<Person>(stream, format, CancellationToken.None))
            {
                Console.WriteLine(item!.Name);
            }

            SampleCheck.Equal(true, stream.CanRead);
            stream.Position = 0;
            await foreach (Person? item in streaming.DeserializeStreamAsync<Person>(stream, format, CancellationToken.None))
            {
                SampleCheck.Equal(person, item!);
            }

            await using MemoryStream bom = new([.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes(body)]);
            int bomCount = 0;
            await foreach (Person? item in streaming.DeserializeStreamAsync<Person>(bom, format, CancellationToken.None))
            {
                SampleCheck.Equal(person, item!);
                bomCount++;
            }

            SampleCheck.Equal(1, bomCount);
        }

        await using MemoryStream implicitToken = new("{\"Id\":1,\"Name\":\"Ada\"} {\"Id\":1,\"Name\":\"Ada\"}"u8.ToArray());
        int count = 0;
        await foreach (Person? item in streaming.DeserializeStreamAsync<Person>(implicitToken, StreamingContentFormat.JsonLines))
        {
            SampleCheck.Equal(person, item!);
            count++;
        }

        SampleCheck.Equal(cases.Length - 1, count);
        await CheckStreamLifetimeAsync(streaming, person);
    }

    /// <summary>Checks cancellation and disposal of a partially consumed reader without closing its input.</summary>
    /// <typeparam name="TSerializer">The streaming serializer capability selected by the caller.</typeparam>
    /// <param name="streaming">The serializer supplying generated metadata.</param>
    /// <param name="person">The expected first item.</param>
    /// <returns>Completion of the lifetime checks.</returns>
    private static async Task CheckStreamLifetimeAsync<TSerializer>(TSerializer streaming, Person person)
        where TSerializer : IStreamingContentSerializer
    {
        using CancellationTokenSource canceled = new();
        await canceled.CancelAsync();
        await using MemoryStream canceledStream = new("[{\"Id\":1,\"Name\":\"Ada\"}]"u8.ToArray());
        bool cancellationObserved = false;
        try
        {
            await foreach (Person? item in streaming.DeserializeStreamAsync<Person>(canceledStream, StreamingContentFormat.JsonArray, canceled.Token))
            {
                SampleCheck.Equal(person, item!);
            }
        }
        catch (OperationCanceledException)
        {
            cancellationObserved = true;
        }

        SampleCheck.Equal(true, cancellationObserved);
        SampleCheck.Equal(true, canceledStream.CanRead);
        await using MemoryStream early = new("[{\"Id\":1,\"Name\":\"Ada\"},{\"Id\":1,\"Name\":\"Ada\"}]"u8.ToArray());
        await using (IAsyncEnumerator<Person?> enumerator = streaming.DeserializeStreamAsync<Person>(early, StreamingContentFormat.JsonArray).GetAsyncEnumerator())
        {
            SampleCheck.Equal(true, await enumerator.MoveNextAsync());
            SampleCheck.Equal(person, enumerator.Current!);
        }

        SampleCheck.Equal(true, early.CanRead);
    }

    /// <summary>Checks direct converter reads and runtime-type writes.</summary>
    /// <param name="person">The runtime object written using generated metadata.</param>
    private static void CheckConverter(Person person)
    {
        ObjectToInferredTypesConverter converter = new();
        Utf8JsonReader reader = new("1"u8);
        _ = reader.Read();
        object? inferred = converter.Read(ref reader, typeof(object), Options);
        Console.WriteLine(inferred!.GetType().Name); // Int64
        SampleCheck.Equal(1L, (long)inferred);
        string[] tokens = ["true", "false", "1.5", "\"2026-01-01T00:00:00\"", "\"Ada\"", "{}", "[]", "null"];
        Type[] expected = [typeof(bool), typeof(bool), typeof(double), typeof(DateTime), typeof(string), typeof(JsonElement), typeof(JsonElement), typeof(JsonElement)];
        object[] expectedValues = [true, false, 1.5, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), "Ada", JsonValueKind.Object, JsonValueKind.Array, JsonValueKind.Null];
        for (int index = 0; index < tokens.Length; index++)
        {
            Utf8JsonReader tokenReader = new(Encoding.UTF8.GetBytes(tokens[index]));
            _ = tokenReader.Read();
            object? value = converter.Read(ref tokenReader, typeof(object), Options);
            SampleCheck.Equal(expected[index], value!.GetType());
            SampleCheck.Equal(expectedValues[index], value is JsonElement element ? element.ValueKind : value);
        }

        using MemoryStream buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            converter.Write(writer, person, Options);
        }

        Console.WriteLine(Encoding.UTF8.GetString(buffer.ToArray()));
        SampleCheck.Equal(PersonJson, Encoding.UTF8.GetString(buffer.ToArray()));
        using MemoryStream emptyObject = new();
        using (Utf8JsonWriter writer = new(emptyObject))
        {
            converter.Write(writer, new(), Options);
        }

        SampleCheck.Equal("{}", Encoding.UTF8.GetString(emptyObject.ToArray()));
    }

    /// <summary>Checks compile-time adapter discovery and a single deferred send.</summary>
    /// <param name="person">The expected reply.</param>
    /// <returns>Completion of the adapter check.</returns>
    private static async Task CheckAdapterAsync(Person person)
    {
        using SampleHost host = new();
        host.Http.Add(new() { Method = HttpMethod.Get, Template = "/content/person" }, Reply.With(person));

        IAdapterApi api = RestService.ForGenerated<IAdapterApi>(host.Client, host.Settings);
        PersonCall<Person> pending = api.Read();
        SampleCheck.Equal(0, host.Http.Requests.Count);
        Person result = await pending.InvokeAsync(CancellationToken.None);
        Console.WriteLine(result.Name); // Ada
        SampleCheck.Equal(person, result);
        SampleCheck.Equal(1, host.Http.Requests.Count);
        await host.Http.VerifyAllCalledAsync();

        using CancellationTokenSource source = new();
        CancellationToken observedToken = default;
        int invocations = 0;
        PersonCallAdapter<Person> adapter = new();
        PersonCall<Person> adapted = adapter.Adapt(token =>
        {
            observedToken = token;
            invocations++;
            return Task.FromResult(person);
        });
        SampleCheck.Equal(0, invocations);
        SampleCheck.Equal(person, await adapted.InvokeAsync(source.Token));
        SampleCheck.Equal(1, invocations);
        SampleCheck.Equal(source.Token, observedToken);
    }
}
