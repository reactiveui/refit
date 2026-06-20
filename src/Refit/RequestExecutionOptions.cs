// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Controls request sending and response handling for generated and reflection-built requests.</summary>
internal readonly struct RequestExecutionOptions : IEquatable<RequestExecutionOptions>
{
    /// <summary>Initializes a new instance of the <see cref="RequestExecutionOptions"/> struct.</summary>
    /// <param name="isApiResponse">Whether the caller expects an API response wrapper.</param>
    /// <param name="shouldDisposeResponse">Whether the response should be disposed by the response processor.</param>
    /// <param name="bufferBody">Whether request content should be buffered before sending.</param>
    /// <param name="applyAuthorizationHeader">Whether to run the authorization-header value getter before sending.</param>
    public RequestExecutionOptions(
        bool isApiResponse,
        bool shouldDisposeResponse,
        bool bufferBody,
        bool applyAuthorizationHeader)
    {
        IsApiResponse = isApiResponse;
        ShouldDisposeResponse = shouldDisposeResponse;
        BufferBody = bufferBody;
        ApplyAuthorizationHeader = applyAuthorizationHeader;
    }

    /// <summary>Gets a value indicating whether the caller expects an API response wrapper.</summary>
    public bool IsApiResponse { get; }

    /// <summary>Gets a value indicating whether the response should be disposed by the response processor.</summary>
    public bool ShouldDisposeResponse { get; }

    /// <summary>Gets a value indicating whether request content should be buffered before sending.</summary>
    public bool BufferBody { get; }

    /// <summary>Gets a value indicating whether the authorization-header getter should run before sending.</summary>
    public bool ApplyAuthorizationHeader { get; }

    /// <inheritdoc/>
    public static bool operator ==(RequestExecutionOptions left, RequestExecutionOptions right) =>
        left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(RequestExecutionOptions left, RequestExecutionOptions right) =>
        !left.Equals(right);

    /// <inheritdoc/>
    public bool Equals(RequestExecutionOptions other) =>
        IsApiResponse == other.IsApiResponse
        && ShouldDisposeResponse == other.ShouldDisposeResponse
        && BufferBody == other.BufferBody
        && ApplyAuthorizationHeader == other.ApplyAuthorizationHeader;

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is RequestExecutionOptions other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(IsApiResponse, ShouldDisposeResponse, BufferBody, ApplyAuthorizationHeader);
}
