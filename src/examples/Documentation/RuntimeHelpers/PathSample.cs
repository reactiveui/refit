// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.RuntimeHelpers;

/// <summary>Uses generator-computed placeholder ranges and request metadata.</summary>
internal static class PathSample
{
    /// <summary>The absolute URL validated from both supported value representations.</summary>
    private const string AbsoluteUrl = "https://example.test/items";

    /// <summary>The path segment used to compare the two resolution modes.</summary>
    private const string ItemSegment = "items";

    /// <summary>The shared client used only to inspect URI resolution.</summary>
    private static readonly HttpClient Client = new() { BaseAddress = new("https://example.test/api/") };

    /// <summary>Checks every path-building overload and the formatting fast-path guards.</summary>
    internal static void Run()
    {
        RefitSettings settings = new()
        {
            HttpRequestMessageOptions = new() { ["Configured"] = "retained" },
            Version = System.Net.HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
        };

        const string template = "/items/{id}";
        const string itemsPath = "/items";
        (int StartIdx, int EndIdx) range = (SampleValues.PlaceholderStart, SampleValues.PlaceholderEnd);
        string escaped = GeneratedRequestRunner.BuildRequestPath(template, false, [(range, "a/b")]);
        string encoded = GeneratedRequestRunner.BuildRequestPath(template, false, [(range, "a%2Fb", true)]);
        string integer = GeneratedRequestRunner.BuildRequestPath(template, false, range, SampleValues.Identifier);
        string formatted = GeneratedRequestRunner.BuildRequestPath(template, false, range, SampleValues.Identifier, "D3");
        string unchanged = GeneratedRequestRunner.BuildRequestPath(itemsPath, false);
        string catchAll = GeneratedRequestRunner.RoundTripEscapePath("a b/c", settings, GeneratedParameterAttributeProvider.Empty, typeof(string));
        string absolute = GeneratedRequestRunner.RequireAbsoluteUrl(new Uri(AbsoluteUrl));

        Check.Require(escaped == "/items/a%2Fb" && encoded == escaped, "String path replacements escape exactly once.");
        Check.Require(integer == "/items/42" && formatted == "/items/042", "Generic paths render integer values invariantly.");
        Check.Require(unchanged == itemsPath && catchAll == "a%20b/c", "Catch-all paths preserve separators.");
        Check.Require(absolute == AbsoluteUrl, "Absolute URLs retain their text.");
        Check.Require(GeneratedRequestRunner.RequireAbsoluteUrl(AbsoluteUrl) == absolute, "String absolute URLs retain their original spelling.");
        string rooted = GeneratedRequestRunner.RequireAbsoluteUrl(itemsPath);
        Check.Require(rooted == itemsPath && new Uri(rooted, UriKind.Absolute).IsFile, "Current absolute-URI validation accepts rooted file paths; it does not restrict values to HTTP URLs.");
        foreach (object? invalid in new object?[] { null, ItemSegment, new Uri(ItemSegment, UriKind.Relative), SampleValues.Identifier })
        {
            bool rejected = false;
            try
            {
                _ = GeneratedRequestRunner.RequireAbsoluteUrl(invalid);
            }
            catch (ArgumentException exception)
            {
                rejected = exception.ParamName == "url";
            }

            Check.Require(rejected, "Null, relative, and unsupported absolute URL values are rejected.");
        }

        Check.Require(GeneratedRequestRunner.BuildRequestPath(template, true) == template, "Allowed unmatched placeholders remain in the path.");
        bool unmatchedRejected = false;
        try
        {
            _ = GeneratedRequestRunner.BuildRequestPath(template, false);
        }
        catch (ArgumentException)
        {
            unmatchedRejected = true;
        }

        Check.Require(unmatchedRejected, "Disallowed unmatched placeholders fail path construction.");
        const string optionalTemplate = "/items/{id?}";
        string optional = GeneratedRequestRunner.BuildRequestPath(optionalTemplate, false, [((range.StartIdx, range.EndIdx + 1), (string?)null)]);
        Check.Require(optional == itemsPath, "A missing optional terminal segment removes its slash.");
        Check.Require(GeneratedRequestRunner.BuildRequestPath(template, false, [(range, (string?)null)]) == "/items/", "A null nonoptional value retains the empty segment's slash.");
        Check.Require(
            GeneratedRequestRunner.RoundTripEscapePath(null, settings, GeneratedParameterAttributeProvider.Empty, typeof(string)).Length == 0,
            "A missing catch-all value contributes an empty fragment.");
        CheckFormatting(settings);
        CheckUris(itemsPath);
        CheckRequestOptions(settings, itemsPath);
    }

    /// <summary>Checks inline-formatting guards and the two-pass collection helper.</summary>
    /// <param name="settings">Pristine formatter settings.</param>
    private static void CheckFormatting(RefitSettings settings)
    {
        Check.Require(GeneratedRequestRunner.UsesDefaultUrlParameterFormatting(settings), "Pristine URL formatting permits inlining.");
        Check.Require(GeneratedRequestRunner.UsesDefaultFormUrlEncodedParameterFormatting(settings), "Pristine form formatting permits inlining.");
        Check.Require(GeneratedRequestRunner.UsesDefaultUrlParameterKeyFormatting(settings), "Pristine key formatting permits inlining.");
        Check.Require(GeneratedRequestRunner.FormatInvariant(SampleValues.Count, "D3") == "012", "Invariant formatting applies the requested format.");
        Check.Require(GeneratedRequestRunner.BuildQueryKey(settings, "Name", "alias", "filter.") == "filter.alias", "Explicit aliases bypass key formatting.");
        RefitSettings camelCase = new() { UrlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter() };
        Check.Require(
            !GeneratedRequestRunner.UsesDefaultUrlParameterKeyFormatting(camelCase) && GeneratedRequestRunner.BuildQueryKey(camelCase, "Name", null, "filter.") == "filter.name",
            "Custom key formatters disable inlining and format unaliased names.");
        DefaultUrlParameterFormatter custom = new();
        custom.AddFormat<int>("D3");
        Check.Require(
            !GeneratedRequestRunner.UsesDefaultUrlParameterFormatting(new() { UrlParameterFormatter = custom }),
            "Configured default URL formatters no longer qualify for the pristine fast path.");
        const string unchangedPath = "/items";
        GeneratedQueryStringBuilder query = new(unchangedPath);
        GeneratedRequestRunner.AddFormattedCollectionProperty(
            ref query,
            settings,
            SampleValues.Items,
            "ids",
            CollectionFormat.Csv,
            false,
            (typeof(int[]), GeneratedParameterAttributeProvider.Empty, typeof(int[])));
        Check.Require(query.Build() == "/items?ids=1%2C2", "Collection properties use two formatting passes.");
        GeneratedQueryStringBuilder multi = new(unchangedPath);
        (Type, System.Reflection.ICustomAttributeProvider, Type) formatting = (typeof(int[]), GeneratedParameterAttributeProvider.Empty, typeof(int[]));
        GeneratedRequestRunner.AddFormattedCollectionProperty(ref multi, settings, null, "absent", CollectionFormat.Multi, false, formatting);
        GeneratedRequestRunner.AddFormattedCollectionProperty(ref multi, settings, SampleValues.Items, "ids", CollectionFormat.Multi, false, formatting);
        Check.Require(multi.Build() == "/items?ids=1&ids=2", "The two-pass helper omits null collections and repeats Multi keys.");
    }

    /// <summary>Checks legacy base-path handling and the RFC format override rule.</summary>
    /// <param name="itemsPath">The relative path with a leading slash.</param>
    private static void CheckUris(string itemsPath)
    {
        Uri legacy = GeneratedRequestRunner.BuildRelativeUri(Client, itemsPath, UrlResolutionMode.RefitLegacy);
        Uri rfc = GeneratedRequestRunner.BuildRelativeUri(Client, ItemSegment, UrlResolutionMode.Rfc3986, UriFormat.Unescaped);
        Uri unescaped = GeneratedRequestRunner.BuildRelativeUri(Client, "/items?q=a%20b", UrlResolutionMode.RefitLegacy, UriFormat.Unescaped);

        Check.Require(legacy.OriginalString == "/api/items", "Legacy resolution retains the base path.");
        Check.Require(rfc.OriginalString == ItemSegment, "RFC resolution leaves the relative path unchanged.");
        Check.Require(unescaped.OriginalString == "/api/items?q=a b", "Legacy QueryUriFormat applies to the complete URI.");
    }

    /// <summary>Checks generated headers and typed request options.</summary>
    /// <param name="settings">The configured request defaults.</param>
    /// <param name="itemsPath">The relative request path.</param>
    private static void CheckRequestOptions(RefitSettings settings, string itemsPath)
    {
        const string modeHeader = "X-Mode";
        const string removedHeader = "X-Remove";
        using HttpRequestMessage request = new(HttpMethod.Post, itemsPath);
        GeneratedRequestRunner.SetHeader(request, modeHeader, "old", validateHeaders: true);
        GeneratedRequestRunner.AddHeaderCollection(request, new Dictionary<string, string> { [modeHeader] = "new" }, validateHeaders: true);
        GeneratedRequestRunner.SetHeader(request, removedHeader, "remove me", validateHeaders: false);
        GeneratedRequestRunner.SetHeader(request, removedHeader, null, validateHeaders: false);
        GeneratedRequestRunner.AddHeaderCollection(request, null, validateHeaders: true);
        GeneratedRequestRunner.AddConfiguredRequestOptions(request, settings, typeof(IHelperApi));
        GeneratedRequestRunner.AddRequestProperty(request, "TraceId", SampleValues.Identifier);
        GeneratedRequestRunner.SetRequestTimeout(request, SampleValues.TimeoutMilliseconds);
        Check.Require(request.Options.TryGetValue(new("TraceId"), out int traceId) && traceId == SampleValues.Identifier, "Typed request properties use options.");
        Check.Require(request.Options.TryGetValue(new("Configured"), out string? configured) && configured == "retained", "Settings options retain their configured value.");
        Check.Require(
            request.Options.TryGetValue(new(HttpRequestMessageOptions.InterfaceType), out Type? interfaceType) && interfaceType == typeof(IHelperApi),
            "Generated requests retain their top-level interface type.");
        Check.Require(request.Options.TryGetValue(new("Refit.Timeout"), out int timeout) && timeout == SampleValues.TimeoutMilliseconds, "Per-request timeout values are stored for dispatch.");
        Check.Require(request.Version == settings.Version && request.VersionPolicy == settings.VersionPolicy, "Configured HTTP version and version policy are applied.");
        int valueCount = 0;
        foreach (string value in request.Headers.GetValues(modeHeader))
        {
            Check.Require(value == "new", "Header collections replace an existing value.");
            valueCount++;
        }

        Check.Require(valueCount == 1 && !request.Headers.Contains(removedHeader), "Header collections do not append duplicates and null removes a header.");
        Check.Require(request.Headers.Contains(modeHeader) && request.Content is not null, "Headers can provision empty POST content.");
    }
}
