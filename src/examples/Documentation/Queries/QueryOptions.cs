// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Globalization;

namespace Refit.Documentation;

/// <summary>Checks query constructors and every scalar collection format.</summary>
internal static class QueryOptions
{
    /// <summary>The prefix used to compare attribute constructor overloads.</summary>
    private const string Prefix = "filter";

    /// <summary>Checks attributes directly and builds the corresponding generated requests.</summary>
    /// <param name="host">The local client and generated JSON metadata configuration.</param>
    /// <returns>A task that completes after all query assertions pass.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        CheckConstructors();

        IQueryOptionsApi api = RestService.ForGenerated<IQueryOptionsApi>(host.Client, host.Settings);
        DateTime day = DateTime.ParseExact("2026-09-17", "yyyy-MM-dd", CultureInfo.InvariantCulture);
        using HttpRequestMessage dates = await api.DatesAsync(new() { Started = day, End = day });
        Console.WriteLine(dates.RequestUri); // /reports?filter-Started=09%2F17%2F2026%2000%3A00%3A00&filter-End=2026
        using HttpRequestMessage amount = await api.AmountAsync(1);
        Console.WriteLine(amount.RequestUri); // /reports?amount=1.00

        SampleCheck.Equal("/reports?filter-Started=09%2F17%2F2026%2000%3A00%3A00&filter-End=2026", dates.RequestUri?.OriginalString);
        SampleCheck.Equal("/reports?amount=1.00", amount.RequestUri?.OriginalString);
        await CheckCollectionsAsync(host);
        await CheckStringObjectsAsync(api);
    }

    /// <summary>Checks the two attribute forms that stringify an object before formatting.</summary>
    /// <param name="api">The generated query client.</param>
    /// <returns>A task that completes after the stringified query is checked.</returns>
    private static async Task CheckStringObjectsAsync(IQueryOptionsApi api)
    {
        using HttpRequestMessage text = await api.TextAsync(new("Ada Lovelace"), new("Grace Hopper"));
        Console.WriteLine(text.RequestUri); // /reports?phrase=Ada%20Lovelace&other=Grace%20Hopper

        SampleCheck.Equal("/reports?phrase=Ada%20Lovelace&other=Grace%20Hopper", text.RequestUri?.OriginalString);
    }

    /// <summary>Checks the distinct default and explicitly assigned collection-format states.</summary>
    private static void CheckConstructors()
    {
        AliasAsAttribute alias = new(Prefix);
        QueryUriFormatAttribute uri = new(UriFormat.Unescaped);
        SampleCheck.Equal(Prefix, alias.Name);
        SampleCheck.Equal(UriFormat.Unescaped, uri.UriFormat);
        QueryAttribute defaults = new();
        QueryAttribute delimiter = new("-");
        QueryAttribute prefixed = new("-", Prefix);
        QueryAttribute formatted = new("-", Prefix, "yyyy-MM");
        QueryAttribute repeated = new(CollectionFormat.Multi) { SerializeNull = true, TreatAsString = true };
        Console.WriteLine(defaults.Delimiter); // .
        Console.WriteLine(defaults.Prefix is null); // True
        Console.WriteLine(defaults.IsCollectionFormatSpecified); // False
        Console.WriteLine(delimiter.Delimiter); // -
        Console.WriteLine(prefixed.Prefix); // filter
        Console.WriteLine(formatted.Format); // yyyy-MM
        Console.WriteLine(repeated.CollectionFormat); // Multi
        Console.WriteLine(repeated.IsCollectionFormatSpecified); // True

        SampleCheck.Equal(".", defaults.Delimiter);
        SampleCheck.Equal(null, defaults.Prefix);
        SampleCheck.Equal(false, defaults.IsCollectionFormatSpecified);
        SampleCheck.Equal("-", delimiter.Delimiter);
        SampleCheck.Equal(Prefix, prefixed.Prefix);
        SampleCheck.Equal("yyyy-MM", formatted.Format);
        SampleCheck.Equal(true, repeated.SerializeNull && repeated.TreatAsString && repeated.IsCollectionFormatSpecified);
    }

    /// <summary>Checks separators and null-element behavior for every scalar collection format.</summary>
    /// <param name="host">The client and serializer to use without modifying shared settings.</param>
    /// <returns>A task that completes after each constructed URI is checked.</returns>
    private static async Task CheckCollectionsAsync(SampleHost host)
    {
        CollectionFormat[] formats =
        [
            CollectionFormat.RefitParameterFormatter, CollectionFormat.Csv,
            CollectionFormat.Ssv, CollectionFormat.Tsv, CollectionFormat.Pipes,
            CollectionFormat.Multi, CollectionFormat.Indexed,
        ];
        List<(CollectionFormat Format, string? Path)> results = [];
        foreach (CollectionFormat format in formats)
        {
            RefitSettings settings = new(host.Settings.ContentSerializer) { CollectionFormat = format };
            IQueryOptionsApi api = RestService.ForGenerated<IQueryOptionsApi>(host.Client, settings);
            using HttpRequestMessage request = await api.TagsAsync(["math", null, "code"]);
            Console.WriteLine($"{format}: {request.RequestUri}");
            results.Add((format, request.RequestUri?.OriginalString));
            using HttpRequestMessage empty = await api.TagsAsync([]);
            SampleCheck.Equal(format == CollectionFormat.Multi ? "/reports" : "/reports?tags=", empty.RequestUri?.OriginalString);
        }

        foreach ((CollectionFormat format, string? path) in results)
        {
            string expected = format switch
            {
                CollectionFormat.Ssv => "/reports?tags=math%20%20code",
                CollectionFormat.Tsv => "/reports?tags=math%09%09code",
                CollectionFormat.Pipes => "/reports?tags=math%7C%7Ccode",
                CollectionFormat.Multi => "/reports?tags=math&tags=code",
                _ => "/reports?tags=math%2C%2Ccode",
            };
            SampleCheck.Equal(expected, path);
        }
    }
}
