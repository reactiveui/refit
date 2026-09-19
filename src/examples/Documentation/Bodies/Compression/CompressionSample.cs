// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.IO.Compression;
using Refit.Testing;

namespace Refit.Documentation.Compression;

/// <summary>Checks .NET 11 per-coding option objects and level-based Zstandard fallback.</summary>
internal static class CompressionSample
{
    /// <summary>The local request path shared by the coding variants.</summary>
    private const string Route = "/compression/person";

    /// <summary>The compact generated-metadata request and reply.</summary>
    private const string PersonJson = """{"id":1,"name":"Ada"}""";

    /// <summary>The gzip token used by header matching and decompression.</summary>
    private const string GzipToken = "gzip";

    /// <summary>The Brotli token used by header matching and decompression.</summary>
    private const string BrotliToken = "br";

    /// <summary>The Zstandard token used by header matching.</summary>
    private const string ZstandardToken = "zstd";

    /// <summary>Runs coding options without a live service.</summary>
    /// <returns>Completion of the round-trip checks.</returns>
    internal static async Task RunAsync()
    {
        using SampleHost host = new();

        RefitSettings settings = new(host.Settings.ContentSerializer)
        {
            RequestCompressionLevel = CompressionLevel.Fastest,
            RequestCompressionOptions = new() { GZip = new(), Brotli = new(), Zstandard = new() { AppendChecksum = true } },
        };
        ICompressionApi api = RestService.ForGenerated<ICompressionApi>(host.Client, settings);
        RequestCompression[] codings = [RequestCompression.GZip, RequestCompression.Brotli, RequestCompression.Zstandard];
        foreach (RequestCompression coding in codings)
        {
            string token = coding switch
            {
                RequestCompression.GZip => GzipToken,
                RequestCompression.Brotli => BrotliToken,
                _ => ZstandardToken,
            };
            AddRoute(host, token);

            settings.RequestCompression = coding;
            Person result = await api.PutAsync(new(1, "Ada"));
            Console.WriteLine(result.Name); // Ada
            SampleCheck.Equal("Ada", result.Name);
        }

        AddRoute(host, ZstandardToken);
        settings.RequestCompressionOptions = null;
        settings.RequestCompression = RequestCompression.Zstandard;
        SampleCheck.Equal("Ada", (await api.PutAsync(new(1, "Ada"))).Name);
        await host.Http.VerifyAllCalledAsync();
    }

    /// <summary>Adds a one-shot route that verifies the coding header and decoded JSON.</summary>
    /// <param name="host">The local generated-metadata host.</param>
    /// <param name="token">The expected content-coding token.</param>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static void AddRoute(SampleHost host, string token) =>
        host.Http.Add(new() { Method = HttpMethod.Post, Template = Route, Headers = [("Content-Encoding", token)], WhereAsync = CheckDecodedAsync }, Reply.Json(PersonJson));

    /// <summary>Decodes the selected compressor and checks the restored JSON.</summary>
    /// <param name="request">The request whose content is inspected.</param>
    /// <returns>Whether the restored JSON matches.</returns>
    private static async Task<bool> CheckDecodedAsync(HttpRequestMessage request)
    {
        await using Stream source = await request.Content!.ReadAsStreamAsync();
        await using Stream decoded = CreateDecoder(source, request.Content);
        using StreamReader reader = new(decoded);
        return await reader.ReadToEndAsync() == PersonJson;
    }

    /// <summary>Selects the stream decoder matching the request's content coding.</summary>
    /// <param name="source">The compressed body stream.</param>
    /// <param name="content">The content headers declaring the coding.</param>
    /// <returns>A decoder that leaves its source open.</returns>
    private static Stream CreateDecoder(Stream source, HttpContent content)
    {
        if (content.Headers.ContentEncoding.Contains(GzipToken))
        {
            return new GZipStream(source, CompressionMode.Decompress, leaveOpen: true);
        }

        return content.Headers.ContentEncoding.Contains(BrotliToken)
            ? new BrotliStream(source, CompressionMode.Decompress, leaveOpen: true)
            : new ZstandardStream(source, CompressionMode.Decompress, leaveOpen: true);
    }
}
