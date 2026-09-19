// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Checks HTTP verbs, prefixes, optional segments, catch-all paths and absolute URLs.</summary>
internal static class Routes
{
    /// <summary>Builds each route example and verifies its method or URI without sending it.</summary>
    /// <param name="host">The local handler and generated-metadata client configuration for these samples.</param>
    /// <returns>A task that completes after the sample assertions pass.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        RouteAttributes.Run();

        IRoutesApi api = RestService.ForGenerated<IRoutesApi>(host.Client, host.Settings);
        using HttpRequestMessage request = await api.GetAsync(1);
        Console.WriteLine(request.Method); // GET
        Console.WriteLine(request.RequestUri); // /v1/people/1

        SampleCheck.Equal("/v1/people/1", request.RequestUri?.OriginalString);
        using HttpRequestMessage post = await api.PostAsync(new(1, "Ada"));
        using HttpRequestMessage put = await api.PutAsync(1, new(1, "Ada"));
        using HttpRequestMessage patch = await api.PatchAsync(1, new(1, "Ada"));
        using HttpRequestMessage delete = await api.DeleteAsync(1);
        using HttpRequestMessage head = await api.HeadAsync(1);
        using HttpRequestMessage options = await api.OptionsAsync();

        SampleCheck.Equal(HttpMethod.Post, post.Method);
        SampleCheck.Equal(HttpMethod.Put, put.Method);
        SampleCheck.Equal(HttpMethod.Patch, patch.Method);
        SampleCheck.Equal(HttpMethod.Delete, delete.Method);
        SampleCheck.Equal(HttpMethod.Head, head.Method);
        SampleCheck.Equal(HttpMethod.Options, options.Method);

        using HttpRequestMessage allPeople = await api.OptionalAsync(null);
        using HttpRequestMessage onePerson = await api.OptionalAsync(1);
        using HttpRequestMessage file = await api.FileAsync("reports/annual report.pdf");
        Console.WriteLine(allPeople.RequestUri); // /v1/people
        Console.WriteLine(onePerson.RequestUri); // /v1/people/1
        Console.WriteLine(file.RequestUri); // /v1/files/reports/annual%20report.pdf

        IAbsoluteApi downloads = RestService.ForGenerated<IAbsoluteApi>(host.Client, host.Settings);

        using HttpRequestMessage download = await downloads.DownloadAsync("https://cdn.example/manual.pdf");
        Console.WriteLine(download.RequestUri); // https://cdn.example/manual.pdf
        SampleCheck.Equal("/v1/people", allPeople.RequestUri?.OriginalString);
        SampleCheck.Equal("/v1/people/1", onePerson.RequestUri?.OriginalString);
        SampleCheck.Equal("/v1/files/reports/annual%20report.pdf", file.RequestUri?.OriginalString);
        SampleCheck.Equal("cdn.example", download.RequestUri?.Host);

        using HttpRequestMessage uriDownload = await downloads.DownloadUriAsync(new("https://cdn.example/manual.pdf"));
        Console.WriteLine(uriDownload.RequestUri); // https://cdn.example/manual.pdf
        SampleCheck.Equal("cdn.example", uriDownload.RequestUri?.Host);
    }
}
