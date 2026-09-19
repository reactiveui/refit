// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.IO.Compression;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Checks constructor, serializer-capability, content coding, URI and timeout policies.</summary>
internal static class BodyPolicies
{
    /// <summary>The positive local deadline used by the annotated method.</summary>
    internal const int TimeoutMilliseconds = 25;

    /// <summary>The compact generated-JSON person body.</summary>
    private const string PersonJson = """{"id":1,"name":"Ada"}""";

    /// <summary>The ordinary JSON route reused for per-mode checks.</summary>
    private const string JsonRoute = "/body/json";

    /// <summary>The content coding header compared by local expectations.</summary>
    private const string CodingHeader = "Content-Encoding";

    /// <summary>The gzip token shared by the coding checks.</summary>
    private const string GzipToken = "gzip";

    /// <summary>Runs focused policy checks against local routes.</summary>
    /// <param name="host">The generated-metadata host shared by body examples.</param>
    /// <returns>Completion of the policy checks.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        VerifyConstructors();
        await CheckModesAsync(host);
        await CheckCompressionAsync(host);
        await CheckResolutionAsync();
        await CheckTimeoutAsync(host.Settings.ContentSerializer);
    }

    /// <summary>Checks all four BodyAttribute constructor shapes and exposed values.</summary>
    private static void VerifyConstructors()
    {
        BodyAttribute inherited = new();
        BodyAttribute buffered = new(true);
        BodyAttribute serialized = new(BodySerializationMethod.Serialized);
        BodyAttribute explicitPolicy = new(BodySerializationMethod.Serialized, false) { Compression = RequestCompression.Brotli, CompressionLevel = CompressionLevel.Fastest };
        Console.WriteLine(inherited.Buffered is null); // True
        Console.WriteLine(explicitPolicy.Buffered); // False
        SampleCheck.Equal(BodySerializationMethod.Default, inherited.SerializationMethod);
        SampleCheck.Equal(RequestCompression.Default, inherited.Compression);
        SampleCheck.Equal(CompressionLevel.Optimal, inherited.CompressionLevel);
        SampleCheck.Equal(true, buffered.Buffered!.Value);
        SampleCheck.Equal(BodySerializationMethod.Serialized, serialized.SerializationMethod);
        SampleCheck.Equal(RequestCompression.Brotli, explicitPolicy.Compression);
        SampleCheck.Equal(CompressionLevel.Fastest, explicitPolicy.CompressionLevel);
    }

    /// <summary>Checks all serializer modes plus fallback when the optional capability is absent.</summary>
    /// <param name="host">The handler and generated serializer used by each request.</param>
    /// <returns>Completion of the mode checks.</returns>
    private static async Task CheckModesAsync(SampleHost host)
    {
        RequestBodySerializationMode[] modes = [RequestBodySerializationMode.Default, RequestBodySerializationMode.Buffered, RequestBodySerializationMode.Streamed];
        foreach (RequestBodySerializationMode mode in modes)
        {
            host.Http.Add(new() { Method = HttpMethod.Post, Template = JsonRoute, Body = PersonJson }, Reply.Json(PersonJson));

            RefitSettings settings = new(host.Settings.ContentSerializer) { RequestBodySerialization = mode };
            IBodyApi api = RestService.ForGenerated<IBodyApi>(host.Client, settings);
            Person result = await api.JsonAsync(new(1, "Ada"));
            Console.WriteLine(result.Name); // Ada
            SampleCheck.Equal("Ada", result.Name);

            host.Http.Add(new() { Method = HttpMethod.Post, Template = JsonRoute, Body = PersonJson }, Reply.Json(PersonJson));

            ContentOnlySerializer limited = new((SystemTextJsonContentSerializer)host.Settings.ContentSerializer);
            RefitSettings fallback = new(limited) { RequestBodySerialization = mode };
            IBodyApi fallbackApi = RestService.ForGenerated<IBodyApi>(host.Client, fallback);
            Person fallbackResult = await fallbackApi.JsonAsync(new(1, "Ada"));
            SampleCheck.Equal("Ada", fallbackResult.Name);
        }

        host.Http.Add(new() { Method = HttpMethod.Post, Template = "/body/buffered", Body = PersonJson }, Reply.Json(PersonJson));
        IBodyPolicyApi policies = RestService.ForGenerated<IBodyPolicyApi>(host.Client, host.Settings);
        SampleCheck.Equal("Ada", (await policies.BufferedAsync(new(1, "Ada"))).Name);
        host.Http.Add(new() { Method = HttpMethod.Post, Template = "/body/form-text", Body = "name%3DAda" }, Reply.Json(PersonJson));
        SampleCheck.Equal("Ada", (await policies.FormTextAsync("name=Ada")).Name);
        host.Http.Add(new() { Method = HttpMethod.Post, Template = "/body/one-line", Body = PersonJson }, Reply.Json(PersonJson));
        SampleCheck.Equal("Ada", (await policies.OneLineAsync(new(1, "Ada"))).Name);
    }

    /// <summary>Checks settings-level coding, attribute overrides, option objects and unsupported Zstandard.</summary>
    /// <param name="host">The local handler and generated serializer.</param>
    /// <returns>Completion of the coding checks.</returns>
    /// <exception cref="InvalidOperationException">The net10 serializer accepts an unsupported coding.</exception>
    private static async Task CheckCompressionAsync(SampleHost host)
    {
        host.Http.Add(new() { Method = HttpMethod.Post, Template = JsonRoute, Headers = [(CodingHeader, GzipToken)], WhereAsync = CheckCompressedAsync }, Reply.Json(PersonJson));
        host.Http.Add(new() { Method = HttpMethod.Post, Template = "/body/brotli", Headers = [(CodingHeader, "br")], WhereAsync = CheckCompressedAsync }, Reply.Json(PersonJson));
        host.Http.Add(
            new() { Method = HttpMethod.Post, Template = "/body/none", Body = PersonJson, Where = static request => request.Content!.Headers.ContentEncoding.Count == 0 },
            Reply.Json(PersonJson));

        RefitSettings settings = new(host.Settings.ContentSerializer)
        {
            RequestCompression = RequestCompression.GZip,
            RequestCompressionLevel = CompressionLevel.Fastest,
            RequestCompressionOptions = new() { GZip = new(), Brotli = new() },
        };
        IBodyApi inherited = RestService.ForGenerated<IBodyApi>(host.Client, settings);
        IBodyPolicyApi overrides = RestService.ForGenerated<IBodyPolicyApi>(host.Client, settings);
        await inherited.JsonAsync(new(1, "Ada"));
        await overrides.BrotliAsync(new(1, "Ada"));
        await overrides.NoneAsync(new(1, "Ada"));

        settings.RequestCompression = RequestCompression.Zstandard;
        try
        {
            await inherited.JsonAsync(new(1, "Ada"));
            throw new InvalidOperationException("Zstandard must be unavailable on net10.");
        }
        catch (PlatformNotSupportedException)
        {
            Console.WriteLine("Use net11 for Zstandard.");
        }
    }

    /// <summary>Compares gzip/Brotli decompression with the expected compact JSON.</summary>
    /// <param name="request">The request inspected without taking ownership.</param>
    /// <returns>Whether the restored body matches.</returns>
    private static async Task<bool> CheckCompressedAsync(HttpRequestMessage request)
    {
        await using Stream compressed = await request.Content!.ReadAsStreamAsync();
        await using Stream decoded = request.Content.Headers.ContentEncoding.Contains(GzipToken)
            ? new GZipStream(compressed, CompressionMode.Decompress, leaveOpen: true)
            : new BrotliStream(compressed, CompressionMode.Decompress, leaveOpen: true);
        using StreamReader reader = new(decoded);
        return await reader.ReadToEndAsync() == PersonJson;
    }

    /// <summary>Checks leading-slash replacement and relative append using a base-address path.</summary>
    /// <returns>Completion of the URI checks.</returns>
    private static async Task CheckResolutionAsync()
    {
        using SampleHost host = new();
        host.Client.BaseAddress = new("https://people.example/root/");
        host.Http.Add(new() { Method = HttpMethod.Get, Template = "/root/child" }, Reply.Json(PersonJson));

        RefitSettings legacy = new(host.Settings.ContentSerializer) { UrlResolution = UrlResolutionMode.RefitLegacy };
        IBodyPolicyApi legacyApi = RestService.ForGenerated<IBodyPolicyApi>(host.Client, legacy);
        await legacyApi.RootedAsync(); // /root/child
        RefitSettings rfc = new(host.Settings.ContentSerializer) { UrlResolution = UrlResolutionMode.Rfc3986 };
        IBodyPolicyApi rfcApi = RestService.ForGenerated<IBodyPolicyApi>(host.Client, rfc);
        host.Http.Add(new() { Method = HttpMethod.Get, Template = "/child" }, Reply.Json(PersonJson));
        await rfcApi.RootedAsync(); // /child
        host.Http.Add(new() { Method = HttpMethod.Get, Template = "/root/child" }, Reply.Json(PersonJson));
        await rfcApi.RelativeAsync(); // /root/child
        await host.Http.VerifyAllCalledAsync();
    }

    /// <summary>Checks that a positive declared deadline cancels the local effective-token wait.</summary>
    /// <param name="serializer">The generated-metadata serializer retained by the call.</param>
    /// <returns>Completion of the cancellation check.</returns>
    /// <exception cref="InvalidOperationException">The declared deadline does not cancel the local wait.</exception>
    private static async Task CheckTimeoutAsync(IHttpContentSerializer serializer)
    {
        RefitSettings settings = new(serializer) { HttpMessageHandlerFactory = static () => new TimeoutHandler() };
        using HttpClient client = RestService.CreateHttpClient("https://people.example", settings);

        TimeoutAttribute timeout = new(TimeoutMilliseconds);
        TimeoutAttribute disabled = new(0);
        TimeoutAttribute negative = new(-1);
        Console.WriteLine(timeout.Milliseconds);
        SampleCheck.Equal(0, disabled.Milliseconds);
        SampleCheck.Equal(-1, negative.Milliseconds);

        IBodyPolicyApi api = RestService.ForGenerated<IBodyPolicyApi>(client, settings);
        try
        {
            await api.TimeoutAsync();
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("The per-call deadline canceled the request.");
            return;
        }

        throw new InvalidOperationException("The per-call deadline must cancel the wait.");
    }
}
