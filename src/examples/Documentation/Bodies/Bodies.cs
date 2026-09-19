// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Checks JSON, text, streams, forms, JSON lines and gzip request bodies against local routes.</summary>
internal static class Bodies
{
    /// <summary>Provides the first person's expected camel-case JSON body.</summary>
    private const string PersonJson = "{\"id\":1,\"name\":\"Ada\"}";

    /// <summary>Provides the second person's expected JSON-line body.</summary>
    private const string SecondPersonJson = "{\"id\":2,\"name\":\"Grace\"}";

    /// <summary>Uses generated metadata for the body models without reflection.</summary>
    private static readonly JsonSerializerOptions Options = new(SampleJsonContext.Default.Options) { TypeInfoResolver = SampleJsonContext.Default, };

    /// <summary>Shares the body serializer configuration across generated clients.</summary>
    private static readonly RefitSettings Settings = new(new SystemTextJsonContentSerializer(Options));

    /// <summary>Sends every body form and verifies stream ownership and matched routes.</summary>
    /// <param name="host">The local handler and generated-metadata client configuration for these samples.</param>
    /// <returns>A task that completes after the sample assertions pass.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        AddBodyRoute(host, "/body/json", PersonJson);
        AddBodyRoute(host, "/body/text", "hello");
        AddBodyRoute(host, "/body/quoted", "\"quoted\"");
        AddBodyRoute(host, "/body/stream", "stream text");
        AddBodyRoute(host, "/body/content", "content text");
        AddBodyRoute(host, "/body/lines", $"{PersonJson}\n{SecondPersonJson}");
        host.Http.Add(
            new() { Method = HttpMethod.Post, Template = "/body/form", FormData = [("name", "Ada Lovelace"), ("Tags", "math"), ("Tags", "code"), ("Note", string.Empty)], },
            Reply.Json(PersonJson));
        host.Http.Add(new() { Method = HttpMethod.Post, Template = "/body/gzip", Headers = [("Content-Encoding", "gzip")], WhereAsync = CheckGzipAsync, }, Reply.Json(PersonJson));

        const int graceId = 2;
        IBodyApi api = RestService.ForGenerated<IBodyApi>(host.Client, Settings);
        Person saved = await api.JsonAsync(new(1, "Ada"));
        await api.TextAsync("hello");
        await api.QuotedAsync("quoted");

        await using MemoryStream stream = new("stream text"u8.ToArray());
        await api.StreamAsync(stream);
        Console.WriteLine(stream.CanRead); // True: the caller still owns the stream.

        using StringContent content = new("content text");
        await api.ContentAsync(content);
        await api.FormAsync(new());
        await api.LinesAsync([new(1, "Ada"), new(graceId, "Grace")]);
        await api.GzipAsync(new(1, "Ada"));
        Console.WriteLine(saved.Name); // Ada

        SampleCheck.Equal(true, stream.CanRead);
        SampleCheck.Equal("Ada", saved.Name);
        await BodyPolicies.RunAsync(host);
        await host.Http.VerifyAllCalledAsync();
    }

    /// <summary>Adds a one-shot POST expectation for the exact body text.</summary>
    /// <param name="host">The local handler and generated-metadata client configuration for these samples.</param>
    /// <param name="path">The route path; catch-all file routes preserve its internal slashes.</param>
    /// <param name="body">The exact UTF-8 request text expected by the local route.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddBodyRoute(SampleHost host, string path, string body) =>
        host.Http.Add(new() { Method = HttpMethod.Post, Template = path, Body = body }, Reply.Json(PersonJson));

    /// <summary>Reads the gzip request body and compares its decompressed JSON with the expected person.</summary>
    /// <param name="request">The request whose content is inspected without taking ownership of the request.</param>
    /// <returns>True when gzip content decodes to the expected JSON; otherwise, false.</returns>
    private static async Task<bool> CheckGzipAsync(HttpRequestMessage request)
    {
        if (request.Content is null)
        {
            return false;
        }

        await using Stream compressed = await request.Content.ReadAsStreamAsync();
        await using GZipStream gzip = new(compressed, CompressionMode.Decompress, leaveOpen: true);
        using StreamReader reader = new(gzip);
        return await reader.ReadToEndAsync() == PersonJson;
    }
}
