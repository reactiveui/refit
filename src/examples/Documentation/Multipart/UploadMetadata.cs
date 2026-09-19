// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Supplies a sealed model sent as one JSON multipart field.</summary>
/// <param name="Title">The title written as a camel-case JSON property.</param>
internal sealed record UploadMetadata(string Title);
