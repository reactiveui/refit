// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Parsed request metadata for one generated Refit method.</summary>
/// <param name="HttpMethod">The HTTP method name.</param>
/// <param name="Path">The relative URL path template.</param>
/// <param name="ResultType">The method result type, unwrapped from Task or ValueTask when applicable.</param>
/// <param name="DeserializedResultType">The response body type used by API response wrappers.</param>
/// <param name="IsApiResponse">Whether the method result is an API response wrapper.</param>
/// <param name="ShouldDisposeResponse">Whether the response should be disposed by the shared runner.</param>
/// <param name="CanGenerateInline">Whether this method is eligible for generated request construction.</param>
/// <param name="StaticHeaders">The static headers parsed from inherited interfaces, the declaring interface, and the method.</param>
/// <param name="Parameters">The parsed request parameter bindings.</param>
internal sealed record RequestModel(
    string HttpMethod,
    string Path,
    string ResultType,
    string DeserializedResultType,
    bool IsApiResponse,
    bool ShouldDisposeResponse,
    bool CanGenerateInline,
    ImmutableEquatableArray<HeaderModel> StaticHeaders,
    ImmutableEquatableArray<RequestParameterModel> Parameters,
    ImmutableEquatableArray<SubPropertyModel> SubProperties)
{
    /// <summary>Gets an empty model used for non-Refit method placeholders.</summary>
    public static RequestModel Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        false,
        true,
        false,
        ImmutableEquatableArray<HeaderModel>.Empty,
        ImmutableEquatableArray<RequestParameterModel>.Empty, 
        ImmutableEquatableArray<SubPropertyModel>.Empty);
}
