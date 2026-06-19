// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Represents one parsed static header.</summary>
/// <param name="Name">The header name.</param>
/// <param name="Value">The header value, or null when the header removes an earlier value.</param>
internal sealed record HeaderModel(
    string Name,
    string? Value);
