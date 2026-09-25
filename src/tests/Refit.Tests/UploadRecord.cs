// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A record uploaded by <see cref="IJsonLinesUploadApi"/>.</summary>
/// <param name="Id">The record's identifier.</param>
/// <param name="Name">The record's name.</param>
internal sealed record UploadRecord(int Id, string Name);
