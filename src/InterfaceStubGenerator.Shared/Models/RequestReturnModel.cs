// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes RequestReturnModel metadata.</summary>
/// <param name="ResultType">The method result type, unwrapped from Task or ValueTask when applicable.</param>
/// <param name="DeserializedResultType">The response body type used by API response wrappers.</param>
/// <param name="IsApiResponse">Whether the method result is an API response wrapper.</param>
/// <param name="ShouldDisposeResponse">Whether the response should be disposed by the shared runner.</param>
internal readonly record struct RequestReturnModel(
    string ResultType,
    string DeserializedResultType,
    bool IsApiResponse,
    bool ShouldDisposeResponse);
