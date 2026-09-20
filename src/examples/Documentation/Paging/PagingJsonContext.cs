// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Documentation.Paging;

/// <summary>Generated JSON metadata for the pages and requests of the JSON cloud service stand-ins.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CosmosQuery))]
[JsonSerializable(typeof(DocumentFeed))]
[JsonSerializable(typeof(GraphUserPage))]
[JsonSerializable(typeof(List<GitHubRepo>))]
[JsonSerializable(typeof(GcsObjectList))]
[JsonSerializable(typeof(JiraSearchResult))]
[JsonSerializable(typeof(DynamoScanRequest))]
[JsonSerializable(typeof(DynamoScanResponse))]
internal sealed partial class PagingJsonContext : JsonSerializerContext;
