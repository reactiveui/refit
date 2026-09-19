// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.RuntimeHelpers;

/// <summary>Builds query strings with the same primitives the generator emits.</summary>
internal static class QuerySample
{
    /// <summary>Checks escaping, omission, span formatting, flags, and collection state.</summary>
    internal static void Run()
    {
        GeneratedQueryStringBuilder builder = new("/search?fixed=1");
        builder.Add("term", "a b", preEncoded: false);
        builder.Add("missing", null, preEncoded: false);
        builder.AddPreEscapedKey("already%20escaped", "x/y", preEncoded: false);
        builder.Add("raw", "a%2Fb", preEncoded: true);
        builder.AddFlag("verbose", preEncoded: false);
        builder.AddFlag(null, preEncoded: false);
        builder.AddFormatted("page", SampleValues.Count, "D3", preEncoded: false);
        builder.AddFormattedPreEscapedKey("row%20count", SampleValues.Rows, null, preEncoded: false);
        string path = builder.Build();

        Check.Require(path == "/search?fixed=1&term=a%20b&already%20escaped=x%2Fy&raw=a%2Fb&verbose&page=012&row%20count=25", "Query pairs preserve order and escape values.");
        const string searchPath = "/search";
        GeneratedQueryStringBuilder collections = new(searchPath, hasQuery: false);
        collections.BeginCollection("tag", CollectionFormat.Multi, preEncoded: false);
        collections.AddCollectionValue("a b");
        collections.AddCollectionValue(null);
        collections.AddCollectionValueFormatted(SampleValues.Element);
        collections.EndCollection();
        collections.BeginCollection("ids", CollectionFormat.Csv, preEncoded: false);
        collections.AddCollectionValueFormatted(1);
        collections.AddCollectionValue(null);
        collections.AddCollectionValue("3");
        collections.EndCollection();
        collections.BeginCollection("empty", CollectionFormat.Pipes, preEncoded: false);
        collections.EndCollection();
        string collectionPath = collections.Build();

        Check.Require(collectionPath == "/search?tag=a%20b&tag=7&ids=1%2C%2C3&empty=", "Collection null and empty semantics match the request builder.");
        GeneratedQueryStringBuilder unchanged = new(searchPath);
        Check.Require(unchanged.Build() == searchPath, "An unused builder preserves its path.");
        GeneratedQueryStringBuilder delimiters = new(searchPath);
        foreach (CollectionFormat format in new[] { CollectionFormat.Ssv, CollectionFormat.Tsv, CollectionFormat.Pipes })
        {
            delimiters.BeginCollection(format.ToString(), format, preEncoded: false);
            delimiters.AddCollectionValue("a");
            delimiters.AddCollectionValue("b");
            delimiters.EndCollection();
        }

        delimiters.BeginCollection("absent", CollectionFormat.Multi, preEncoded: false);
        delimiters.EndCollection();
        delimiters.BeginCollection("encoded", CollectionFormat.Multi, preEncoded: true);
        delimiters.AddCollectionValue("a%2Fb");
        delimiters.EndCollection();
        Check.Require(delimiters.Build() == "/search?Ssv=a%20b&Tsv=a%09b&Pipes=a%7Cb&encoded=a%2Fb", "Joined delimiters are escaped, empty Multi is omitted, and encoded values are retained.");
        GeneratedQueryStringBuilder explicitQuery = new("/search?fixed=1", hasQuery: true);
        explicitQuery.Add("empty key", string.Empty, preEncoded: false);
        explicitQuery.AddFormatted("page%20count", SampleValues.Count, "D3", preEncoded: true);
        Check.Require(explicitQuery.Build() == "/search?fixed=1&empty%20key=&page%20count=012", "Known query state uses ampersands and empty strings remain present.");
    }
}
