// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A request the paged fixture server received.</summary>
/// <param name="Uri">The absolute request URI.</param>
/// <param name="Authorization">The Authorization header value the request carried, or <see langword="null"/>.</param>
/// <param name="Continuation">The <c>x-ms-continuation</c> header value the request carried, or <see langword="null"/>.</param>
internal sealed record RecordedRequest(Uri Uri, string? Authorization, string? Continuation);
