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
/// <param name="AdapterTypeExpression">The closed <c>IReturnTypeAdapter</c> type expression when the return type is
/// surfaced by an adapter, so the generated method emits an <c>Adapt</c> call; otherwise <see langword="null"/>. The
/// result-type fields already carry the adapter's wrapped result type.</param>
/// <param name="StaticHeaders">The static headers parsed from inherited interfaces, the declaring interface, and the method.</param>
/// <param name="Parameters">The parsed request parameter bindings.</param>
internal readonly record struct RequestModel(
    string HttpMethod,
    string Path,
    string ResultType,
    string DeserializedResultType,
    bool IsApiResponse,
    bool ShouldDisposeResponse,
    bool CanGenerateInline,
    string? AdapterTypeExpression,
    ImmutableEquatableArray<HeaderModel> StaticHeaders,
    ImmutableEquatableArray<RequestParameterModel> Parameters)
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
        null,
        ImmutableEquatableArray<HeaderModel>.Empty,
        ImmutableEquatableArray<RequestParameterModel>.Empty);

    /// <summary>Gets a value indicating whether the method is a <c>[Multipart]</c> request whose form parts are
    /// constructed inline. When set, the generated method builds a <c>MultipartFormDataContent</c> as the request body
    /// instead of following the normal single-body path.</summary>
    public bool IsMultipart { get; init; }

    /// <summary>Gets the multipart boundary text, used only when <see cref="IsMultipart"/> is set. Mirrors the
    /// reflection builder's boundary selection: the <c>[Multipart(boundary)]</c> argument, or the attribute default.</summary>
    public string MultipartBoundary { get; init; } = string.Empty;

    /// <summary>Gets the <c>System.UriFormat</c> value from the method's <c>[QueryUriFormat]</c> attribute, or null when
    /// absent. When set, the built path and query are re-encoded with this mode, matching the reflection builder's final
    /// <c>Uri.GetComponents(PathAndQuery, QueryUriFormat)</c> pass.</summary>
    public int? QueryUriFormat { get; init; }

    /// <summary>Gets the per-call timeout in milliseconds from the method's <c>[Timeout]</c> attribute, or 0 when absent.
    /// The value is emitted into the generated send call and layered onto the request's effective cancellation token,
    /// mirroring the reflection builder's <c>RestMethodInfoInternal.TimeoutMilliseconds</c>.</summary>
    public int TimeoutMilliseconds { get; init; }
}
