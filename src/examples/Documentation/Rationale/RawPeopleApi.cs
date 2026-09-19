// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Globalization;
using System.Net.Http.Json;

namespace Refit.Documentation;

/// <summary>Implements the request and response steps hidden by the generated Refit client.</summary>
internal static class RawPeopleApi
{
    /// <summary>Checks HTTP success and deserializes a person with generated JSON metadata.</summary>
    /// <param name="client">The caller-owned client used to send the request.</param>
    /// <param name="id">The person identifier inserted into the route; an optional null identifier omits its segment.</param>
    /// <param name="cancellationToken">The token that cancels request execution and response reading.</param>
    /// <returns>The deserialized person, or null when the response contains JSON null.</returns>
    internal static async Task<Person?> GetPersonAsync(HttpClient client, int id, CancellationToken cancellationToken)
    {
        string path = string.Create(CultureInfo.InvariantCulture, $"/people/{id}");
        using HttpRequestMessage request = new(HttpMethod.Get, path);
        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(SampleJsonContext.Default.Person, cancellationToken);
    }
}
