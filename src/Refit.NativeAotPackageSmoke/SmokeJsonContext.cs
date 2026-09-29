// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Refit.NativeAotPackageSmoke;

/// <summary>The JSON metadata for every body the smoke API sends or reads, except <see cref="Undescribed"/>.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(Person))]
[JsonSerializable(typeof(List<Person>))]
[JsonSerializable(typeof(Failure))]
[JsonSerializable(typeof(PhotoMetadata))]
[JsonSerializable(typeof(PersonPage))]
internal sealed partial class SmokeJsonContext : JsonSerializerContext;
