// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A note attached to a stock line, described only by <see cref="StockNoteJsonContext"/>.</summary>
/// <param name="Text">The note text.</param>
internal sealed record StockNote(string Text);
