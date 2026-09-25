// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>A row of a bulk import, uploaded typed as JSON Lines. Sealed, so the typed path always applies.</summary>
/// <param name="Id">The row's identifier in the import batch.</param>
/// <param name="Sku">The stock-keeping unit for the imported line.</param>
/// <param name="Quantity">The quantity imported for the line.</param>
internal sealed record ImportRecord(int Id, string Sku, int Quantity);
