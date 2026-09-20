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

/// <summary>A stand-in for S3 ListObjectsV2 that serves seven keys as XML and pages them with opaque continuation tokens.</summary>
internal static class S3Mock
{
    /// <summary>The origin of the regional S3 endpoint.</summary>
    internal const string Origin = "https://s3.us-east-1.amazonaws.com";

    /// <summary>The number of keys the bucket holds.</summary>
    internal const int KeyCount = 7;

    /// <summary>The prefix of every key in the bucket.</summary>
    private const string KeyPrefix = "2026/photo-";

    /// <summary>The size of each object, per key number.</summary>
    private const int BytesPerKey = 1024;

    /// <summary>The text an S3 token encodes before it is made opaque.</summary>
    private const string TokenPrefix = "position:";

    /// <summary>Answers a ListObjectsV2 request.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The XML response.</returns>
    internal static HttpResponseMessage Serve(HttpRequestMessage request)
    {
        NameValueCollection query = HttpUtility.ParseQueryString(request.RequestUri!.Query);
        int maxKeys = int.Parse(query["max-keys"]!, CultureInfo.InvariantCulture);
        int start = string.IsNullOrEmpty(query["continuation-token"]) ? 0 : Decode(query["continuation-token"]!);
        int end = Math.Min(start + maxKeys, KeyCount);
        XNamespace ns = ListBucketResult.XmlNamespace;

        XElement root = new(
            ns + nameof(ListBucketResult),
            new XElement(ns + nameof(ListBucketResult.Name), request.RequestUri.AbsolutePath.Trim('/')),
            new XElement(ns + nameof(ListBucketResult.Prefix), query["prefix"]),
            new XElement(ns + nameof(ListBucketResult.KeyCount), end - start),
            new XElement(ns + nameof(ListBucketResult.MaxKeys), maxKeys),
            new XElement(ns + nameof(ListBucketResult.IsTruncated), end < KeyCount ? "true" : "false"));
        if (end < KeyCount)
        {
            root.Add(new XElement(ns + nameof(ListBucketResult.NextContinuationToken), Encode(end)));
        }

        for (int index = start; index < end; index++)
        {
            root.Add(new XElement(
                ns + nameof(ListBucketResult.Contents),
                new XElement(ns + nameof(S3Object.Key), $"{KeyPrefix}{index + 1:000}.jpg"),
                new XElement(ns + nameof(S3Object.Size), (index + 1) * BytesPerKey),
                new XElement(ns + nameof(S3Object.StorageClass), "STANDARD")));
        }

        return new(HttpStatusCode.OK) { Content = new StringContent(new XDocument(new XDeclaration("1.0", "UTF-8", null), root).ToString(), Encoding.UTF8, "application/xml") };
    }

    /// <summary>Makes a position an opaque token that needs URL escaping, like a real S3 token.</summary>
    /// <param name="position">The index of the next key.</param>
    /// <returns>The token.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string Encode(int position) => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{TokenPrefix}{position}"));

    /// <summary>Reads the position from a token.</summary>
    /// <param name="token">The token.</param>
    /// <returns>The index of the next key.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Decode(string token) => int.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(token))[TokenPrefix.Length..], CultureInfo.InvariantCulture);
}
