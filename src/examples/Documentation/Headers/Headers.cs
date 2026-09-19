// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Checks dynamic headers, authorization and request metadata before sending.</summary>
internal static class Headers
{
    /// <summary>The dynamic application header value.</summary>
    private const string App = "Driver";

    /// <summary>The header supplied through the dynamic header collection.</summary>
    private const string RegionHeader = "X-Region";

    /// <summary>The byte count used to generate a test-only authorization token.</summary>
    private const int TokenBytes = 32;

    /// <summary>The token checked by both explicit authorization and the token getter.</summary>
    private static readonly string SampleToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenBytes));

    /// <summary>Checks header replacement, request properties and asynchronous token lookup.</summary>
    /// <param name="host">The shared client and JSON serializer used for the person reply.</param>
    /// <returns>A task that completes after the constructed request and sent reply are checked.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        CheckAttributeMetadata();
        await CheckHeaderValidationAsync(host);
        RefitSettings settings = new(host.Settings.ContentSerializer) { CaptureMethodArguments = true, HttpRequestMessageOptions = new() { ["trace-category"] = "delivery" } };
        IHeaderApi api = RestService.ForGenerated<IHeaderApi>(host.Client, settings);

        using HttpRequestMessage request = await api.BuildAsync(App, new Dictionary<string, string> { [RegionHeader] = "north" }, SampleToken, "customer-a", 1);
        Console.WriteLine(string.Join(",", request.Headers.GetValues("X-App"))); // Driver
        Console.WriteLine(string.Join(",", request.Headers.GetValues(RegionHeader))); // north
        Console.WriteLine(request.Headers.Authorization); // Bearer followed by the sample token
        Console.WriteLine(request.Headers.Contains("X-Remove")); // False

        SampleCheck.Equal(App, string.Join(",", request.Headers.GetValues("X-App")));
        SampleCheck.Equal("north", string.Join(",", request.Headers.GetValues(RegionHeader)));
        SampleCheck.Equal($"Bearer {SampleToken}", request.Headers.Authorization?.ToString());
        SampleCheck.Equal(false, request.Headers.Contains("X-Remove"));

        _ = request.Options.TryGetValue(new("tenant"), out string? tenant);
        _ = request.Options.TryGetValue(new("page"), out int page);
        _ = request.Options.TryGetValue(new(HttpRequestMessageOptions.MethodName), out string? method);
        _ = request.Options.TryGetValue(new(HttpRequestMessageOptions.RelativePathTemplate), out string? route);
        Console.WriteLine(tenant); // customer-a
        Console.WriteLine(page); // 1
        Console.WriteLine(method); // BuildAsync
        Console.WriteLine(route); // /headers

        SampleCheck.Equal("customer-a", tenant);
        SampleCheck.Equal(1, page);
        SampleCheck.Equal(nameof(IHeaderApi.BuildAsync), method);
        SampleCheck.Equal("/headers", route);
        _ = request.Options.TryGetValue(new("trace-category"), out string? category);
        SampleCheck.Equal("delivery", category);
        _ = request.Options.TryGetValue(new(HttpRequestMessageOptions.MethodArguments), out object?[]? arguments);
        SampleCheck.Equal(App, arguments?[0]);

        Person expected = new(1, "Ada");
        host.Http.Add(new() { Method = HttpMethod.Get, Template = "/header-person", Headers = [("Authorization", $"Bearer {SampleToken}")] }, Reply.With(expected));

        RefitSettings tokenSettings = new(host.Settings.ContentSerializer) { AuthorizationHeaderValueGetter = static (_, _) => ValueTask.FromResult(SampleToken) };
        IHeaderApi securedApi = RestService.ForGenerated<IHeaderApi>(host.Client, tokenSettings);
        Person person = await securedApi.ReadAsync(CancellationToken.None);
        Console.WriteLine(person.Name); // Ada

        SampleCheck.Equal(expected, person);
        await host.Http.VerifyAllCalledAsync();
        await RequestContext.RunAsync(host);
    }

    /// <summary>Checks the metadata retained by the header and local-context attributes.</summary>
    private static void CheckAttributeMetadata()
    {
        HeaderAttribute header = new(RegionHeader);
        string[] values = ["X-Region: north"];
        HeadersAttribute headers = new(values);
        HeadersAttribute empty = new(null!);
        AuthorizeAttribute bearer = new();
        AuthorizeAttribute basic = new("Basic");
        PropertyAttribute explicitKey = new("context");
        PropertyAttribute inferredKey = new();
        SampleCheck.Equal(RegionHeader, header.Header);
        SampleCheck.Equal(values, headers.Headers);
        SampleCheck.Equal(0, empty.Headers.Length);
        SampleCheck.Equal("Bearer", bearer.Scheme);
        SampleCheck.Equal("Basic", basic.Scheme);
        SampleCheck.Equal("context", explicitKey.Key);
        SampleCheck.Equal(null, inferredKey.Key);
    }

    /// <summary>Checks opt-in parsing of a malformed standard header before sending.</summary>
    /// <param name="host">The local client and generated JSON settings.</param>
    /// <returns>Completion after permissive and validating requests are compared.</returns>
    private static async Task CheckHeaderValidationAsync(SampleHost host)
    {
        const string malformedAgent = "broken(";
        foreach (bool validate in new[] { false, true })
        {
            RefitSettings settings = new(host.Settings.ContentSerializer) { ValidateHeaders = validate };
            IHeaderApi api = RestService.ForGenerated<IHeaderApi>(host.Client, settings);
            bool rejected = false;
            try
            {
                using HttpRequestMessage request = await api.BuildAsync(App, new Dictionary<string, string> { ["User-Agent"] = malformedAgent }, SampleToken, "local", 1);
                SampleCheck.Equal(malformedAgent, string.Join(',', request.Headers.GetValues("User-Agent")));
            }
            catch (FormatException)
            {
                rejected = true;
            }

            SampleCheck.Equal(validate, rejected);
        }
    }
}
