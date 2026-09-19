// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Stores stable values before the generated client disposes its request.</summary>
/// <param name="Name">The field name without surrounding quotes.</param>
/// <param name="FileName">The optional transmitted file name without surrounding quotes.</param>
/// <param name="MediaType">The optional content media type.</param>
/// <param name="Body">The decoded text of these UTF-8 sample parts.</param>
internal sealed record CapturedPart(string? Name, string? FileName, string? MediaType, string Body);
