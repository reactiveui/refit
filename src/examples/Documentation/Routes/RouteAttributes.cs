// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Checks the stored metadata and protected HTTP method extension point.</summary>
internal static class RouteAttributes
{
    /// <summary>The route stored by each built-in verb attribute.</summary>
    private const string Path = "/people";

    /// <summary>Constructs every verb attribute and checks its method and path.</summary>
    internal static void Run()
    {
        PathPrefixAttribute prefix = new("/v1");
        SampleCheck.Equal("/v1", prefix.Prefix);
        HttpMethodAttribute[] verbs =
        [
            new GetAttribute(Path), new PostAttribute(Path),
            new PutAttribute(Path), new PatchAttribute(Path),
            new DeleteAttribute(Path), new HeadAttribute(Path),
            new OptionsAttribute(Path),
        ];
        foreach (HttpMethodAttribute verb in verbs)
        {
            Console.WriteLine($"{verb.Method} {verb.Path}");
        }

        SearchAttribute search = new("/old-search");
        search.ChangePath("/search");
        Console.WriteLine($"{search.Method} {search.Path}"); // SEARCH /search

        List<string> methods = [];
        foreach (HttpMethodAttribute verb in verbs)
        {
            methods.Add(verb.Method.Method);
            SampleCheck.Equal(Path, verb.Path);
        }

        SampleCheck.Equal("GET,POST,PUT,PATCH,DELETE,HEAD,OPTIONS", string.Join(',', methods));
        SampleCheck.Equal("/search", search.Path);
        SampleCheck.Equal("SEARCH", search.Method.Method);
    }
}
