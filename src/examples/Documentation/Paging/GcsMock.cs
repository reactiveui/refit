// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Specialized;
using System.Globalization;
using System.Web;

namespace Refit.Documentation.Paging;

/// <summary>A stand-in for the Google Cloud Storage JSON API that serves seven objects and pages them with tokens.</summary>
internal static class GcsMock
{
    /// <summary>The origin of the Google Cloud Storage JSON API.</summary>
    internal const string Origin = "https://storage.googleapis.com";

    /// <summary>The number of objects the bucket holds.</summary>
    internal const int ObjectCount = 7;

    /// <summary>The size of each object, per object number.</summary>
    private const int BytesPerObject = 100;

    /// <summary>The text a page token starts with.</summary>
    private const string TokenPrefix = "CgtvYmplY3Q";

    /// <summary>Answers an objects request.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The JSON response.</returns>
    internal static HttpResponseMessage Serve(HttpRequestMessage request)
    {
        NameValueCollection query = HttpUtility.ParseQueryString(request.RequestUri!.Query);
        int maxResults = int.Parse(query["maxResults"]!, CultureInfo.InvariantCulture);
        int start = string.IsNullOrEmpty(query["pageToken"]) ? 0 : int.Parse(query["pageToken"]![TokenPrefix.Length..], CultureInfo.InvariantCulture);
        int end = Math.Min(start + maxResults, ObjectCount);

        GcsObjectList list = new() { Kind = "storage#objects" };
        for (int index = start; index < end; index++)
        {
            list.Items.Add(new() { Name = $"{query["prefix"]}report-{index + 1}.csv", Size = ((index + 1) * BytesPerObject).ToString(CultureInfo.InvariantCulture) });
        }

        if (end < ObjectCount)
        {
            list.NextPageToken = $"{TokenPrefix}{end}";
        }

        return PagingReplies.Json(list, PagingJsonContext.Default.GcsObjectList);
    }
}
