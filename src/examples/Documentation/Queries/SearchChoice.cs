// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Documentation;

/// <summary>Carries the search values consumed by the custom query converter.</summary>
/// <param name="Name">The search text emitted by the custom converter.</param>
/// <param name="Limit">The result limit formatted as an invariant integer.</param>
internal sealed record SearchChoice(string Name, int Limit);
