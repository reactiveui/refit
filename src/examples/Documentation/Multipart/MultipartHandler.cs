// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;

namespace Refit.Documentation;

/// <summary>Reads multipart content locally and retains snapshots of its values.</summary>
internal sealed class MultipartHandler : HttpMessageHandler
{
    /// <summary>Gets the parts captured from the most recent request.</summary>
    internal List<CapturedPart> Parts { get; private set; } = [];

    /// <summary>Gets the boundary captured from the most recent body.</summary>
    internal string? Boundary { get; private set; }

    /// <summary>Gets the query text captured from the most recent request.</summary>
    internal string? Query { get; private set; }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        MultipartFormDataContent multipart = (MultipartFormDataContent)request.Content!;
        foreach (System.Net.Http.Headers.NameValueHeaderValue parameter in multipart.Headers.ContentType!.Parameters)
        {
            if (parameter.Name == "boundary")
            {
                Boundary = parameter.Value!.Trim('"');
            }
        }

        Query = request.RequestUri!.Query;
        List<CapturedPart> parts = [];
        foreach (HttpContent part in multipart)
        {
            parts.Add(new(
                part.Headers.ContentDisposition?.Name?.Trim('"'),
                part.Headers.ContentDisposition?.FileName?.Trim('"'),
                part.Headers.ContentType?.MediaType,
                await part.ReadAsStringAsync(cancellationToken)));
        }

        Parts = parts;
        return new(HttpStatusCode.OK) { RequestMessage = request };
    }
}
