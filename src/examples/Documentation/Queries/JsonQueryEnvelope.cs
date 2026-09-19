// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Combines a nested object, collection and omitted null for JSON query flattening.</summary>
/// <param name="Person">The nested person.</param>
/// <param name="Codes">The repeated scalar values.</param>
/// <param name="Missing">The optional object omitted when null.</param>
internal sealed record JsonQueryEnvelope(Person Person, int[] Codes, Person? Missing);
