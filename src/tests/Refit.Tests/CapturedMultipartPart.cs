// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A multipart part captured while a request is in flight.</summary>
/// <param name="Name">The content-disposition name of the part, if any.</param>
/// <param name="Body">The part body read as a string.</param>
public sealed record CapturedMultipartPart(string? Name, string Body);
