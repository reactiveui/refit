// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using System.Xml.Linq;

namespace Refit.Documentation.Paging;

/// <summary>A stand-in for Azure Blob Storage List Blobs that serves seven blobs as XML and pages them with markers.</summary>
internal static class AzureBlobMock
{
    /// <summary>The origin of a storage account's blob endpoint.</summary>
    internal const string Origin = "https://contoso.blob.core.windows.net";

    /// <summary>The number of blobs the container holds.</summary>
    internal const int BlobCount = 7;

    /// <summary>The query parameter that carries the marker.</summary>
    private const string MarkerParameter = "marker";

    /// <summary>The text a marker encodes before it is made opaque.</summary>
    private const string MarkerPrefix = "position:";

    /// <summary>The version of the marker format Azure reports at the start of a marker.</summary>
    private const string MarkerVersion = "2";

    /// <summary>The number of parts in a marker: the version, the payload length and the payload.</summary>
    private const int MarkerParts = 3;

    /// <summary>Answers a List Blobs request.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The XML response.</returns>
    internal static HttpResponseMessage Serve(HttpRequestMessage request)
    {
        NameValueCollection query = HttpUtility.ParseQueryString(request.RequestUri!.Query);
        int maxResults = int.Parse(query["maxresults"]!, CultureInfo.InvariantCulture);
        int start = string.IsNullOrEmpty(query[MarkerParameter]) ? 0 : Decode(query[MarkerParameter]!);
        int end = Math.Min(start + maxResults, BlobCount);

        XElement blobs = new(nameof(EnumerationResults.Blobs));
        for (int index = start; index < end; index++)
        {
            blobs.Add(new XElement(nameof(Blob), new XElement(nameof(Blob.Name), $"{query["prefix"]}blob-{index + 1:000}.log")));
        }

        // The last page carries an empty NextMarker element rather than omitting it.
        XElement root = new(
            nameof(EnumerationResults),
            new XAttribute(nameof(EnumerationResults.ContainerName), request.RequestUri.AbsolutePath.Trim('/')),
            new XElement(nameof(EnumerationResults.Prefix), query["prefix"]),
            new XElement(nameof(EnumerationResults.Marker), query[MarkerParameter]),
            new XElement(nameof(EnumerationResults.MaxResults), maxResults),
            blobs,
            new XElement(nameof(EnumerationResults.NextMarker), end < BlobCount ? Encode(end) : string.Empty));

        return new(HttpStatusCode.OK) { Content = new StringContent(new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString(), Encoding.UTF8, "application/xml") };
    }

    /// <summary>Makes a position an opaque marker containing characters that need URL escaping.</summary>
    /// <param name="position">The index of the next blob.</param>
    /// <returns>The marker.</returns>
    private static string Encode(int position)
    {
        string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{MarkerPrefix}{position}"));
        return $"{MarkerVersion}!{payload.Length}!{payload}";
    }

    /// <summary>Reads the position from a marker.</summary>
    /// <param name="marker">The marker.</param>
    /// <returns>The index of the next blob.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Decode(string marker) =>
        int.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(marker.Split('!', MarkerParts)[MarkerParts - 1]))[MarkerPrefix.Length..], CultureInfo.InvariantCulture);
}
