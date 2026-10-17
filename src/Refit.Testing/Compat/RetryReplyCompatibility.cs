// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Testing;

/// <summary>Provides retry status values on targets with older HTTP enum definitions.</summary>
internal static class RetryReplyCompatibility
{
    /// <summary>The HTTP rate-limit status.</summary>
    internal const HttpStatusCode TooManyRequests =
#if NET8_0_OR_GREATER
        HttpStatusCode.TooManyRequests;
#else
        (HttpStatusCode)429;
#endif
}
