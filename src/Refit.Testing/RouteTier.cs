// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Testing;

/// <summary>The priority tier a stubbed route is matched in; tiers are tried in ascending order.</summary>
internal enum RouteTier
{
    /// <summary>A one-shot expectation, matched first and consumed on match.</summary>
    OneShot,

    /// <summary>A reusable background stub, matched after one-shot expectations.</summary>
    Reusable,

    /// <summary>A catch-all fallback, matched only when no other route matches.</summary>
    Fallback,
}
