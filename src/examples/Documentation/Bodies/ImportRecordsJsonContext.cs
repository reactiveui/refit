// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Refit.Documentation;

/// <summary>
/// Generated JSON metadata for the bulk-import records, registered so a JSON Lines upload of <see cref="ImportRecord"/>
/// stays AOT-friendly: <see cref="RestService.ForGenerated{T}(HttpClient, JsonSerializerContext)"/> never falls back to
/// reflection for a type this context describes.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ImportRecord))]
[JsonSerializable(typeof(List<ImportRecord>))]
internal sealed partial class ImportRecordsJsonContext : JsonSerializerContext;
