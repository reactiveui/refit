// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Documentation;

/// <summary>Checks generated query strings without sending the built requests.</summary>
internal static class Queries
{
    /// <summary>Identifies the second collection entry in the expected query string.</summary>
    private const int Second = 2;

    /// <summary>Builds and verifies the query forms shown in the documentation.</summary>
    /// <param name="host">The local handler and generated-metadata client configuration for these samples.</param>
    /// <returns>A task that completes after the sample assertions pass.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        IQueryApi api = RestService.ForGenerated<IQueryApi>(host.Client, host.Settings);

        using HttpRequestMessage search = await api.SearchAsync("Ada Lovelace", 1, null);
        Console.WriteLine(search.RequestUri); // /people?active=true&q=Ada%20Lovelace&page=1

        SampleCheck.Equal("/people?active=true&q=Ada%20Lovelace&page=1", search.RequestUri?.OriginalString);

        IQueryApi snakeApi = RestService.ForGenerated<IQueryApi>(host.Client, RefitSettings.SnakeCase());
        using HttpRequestMessage grouped = await snakeApi.FilterAsync(new());
        Console.WriteLine(grouped.RequestUri); // /people?filter.name=Ada%20Lovelace&filter.page_size=20&filter.price=5.00&filter.note=

        SampleCheck.Equal("/people?filter.name=Ada%20Lovelace&filter.page_size=20&filter.price=5.00&filter.note=", grouped.RequestUri?.OriginalString);

        using HttpRequestMessage collection = await api.CollectionsAsync([1, Second], ["math", "code"], [new(1, "Ada"), new(Second, "Grace")]);
        Console.WriteLine(collection.RequestUri); // /people?ids=1&ids=2&tags=math%2Ccode&people[0].Id=1&people[0].Name=Ada&people[1].Id=2&people[1].Name=Grace

        SampleCheck.Equal("/people?ids=1&ids=2&tags=math%2Ccode&people[0].Id=1&people[0].Name=Ada&people[1].Id=2&people[1].Name=Grace", collection.RequestUri?.OriginalString);

        using HttpRequestMessage flagged = await api.FlagsAsync(["preview", null, "include notes"], "Ada%20Lovelace");
        Console.WriteLine(flagged.RequestUri); // /people?preview&include%20notes&q=Ada%20Lovelace

        SampleCheck.Equal("/people?preview&include%20notes&q=Ada%20Lovelace", flagged.RequestUri?.OriginalString);
        using HttpRequestMessage unescaped = await api.UnescapedAsync("Ada Lovelace");
        Console.WriteLine(unescaped.RequestUri?.OriginalString); // /people?q=Ada Lovelace
        SampleCheck.Equal("/people?q=Ada Lovelace", unescaped.RequestUri?.OriginalString);
        await QueryOptions.RunAsync(host);
        await CheckSerializerNamesAsync(host);
    }

    /// <summary>Checks switching between explicit JSON names and formatted CLR names.</summary>
    /// <param name="host">The local client and generated JSON serializer.</param>
    /// <returns>Completion after both naming policies are checked.</returns>
    private static async Task CheckSerializerNamesAsync(SampleHost host)
    {
        foreach (bool honor in new[] { true, false })
        {
            RefitSettings settings = new(host.Settings.ContentSerializer) { HonorContentSerializerPropertyNamesInQuery = honor, UrlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter() };
            IQueryApi api = RestService.ForGenerated<IQueryApi>(host.Client, settings);
            using HttpRequestMessage request = await api.JsonNamesAsync(new() { Value = 1 });
            SampleCheck.Equal(honor ? "/people?wire-name=1" : "/people?value=1", request.RequestUri?.OriginalString);
        }
    }
}
