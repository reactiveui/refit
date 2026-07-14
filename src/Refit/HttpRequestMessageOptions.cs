// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit;

/// <summary>Contains Refit-defined properties on the HttpRequestMessage.Properties/Options.</summary>
public static class HttpRequestMessageOptions
{
    /// <summary>Gets the property key for the top-level interface type the method was called from.</summary>
    public static string InterfaceType { get; } = "Refit.InterfaceType";

    /// <summary>Gets the property key for the rest method info of the top-level interface.</summary>
    public static string RestMethodInfo { get; } = "Refit.RestMethodInfo";

    /// <summary>Gets the property key for the Refit interface method's name. Populated on every request.</summary>
    public static string MethodName { get; } = "Refit.MethodName";

    /// <summary>Gets the property key for the raw route template with <c>{placeholders}</c> (not the filled URL). This is
    /// the low-cardinality value suited to logging, metrics, and tracing. Populated on every request.</summary>
    public static string RelativePathTemplate { get; } = "Refit.RelativePathTemplate";

    /// <summary>Gets the property key under which the captured request body string is stored for <see cref="ApiExceptionBase.RequestContent"/>.</summary>
    public static string RequestContent { get; } = "Refit.RequestContent";

    /// <summary>Gets the property key under which the current call's argument values are stored as an <c>object?[]</c>.</summary>
    /// <remarks>
    /// The array holds the boxed argument values in the method's declared parameter order (including any
    /// <see cref="System.Threading.CancellationToken"/>), so it aligns 1:1 with the reflected
    /// <see cref="System.Reflection.MethodBase.GetParameters"/>. It is only populated when
    /// <see cref="RefitSettings.CaptureMethodArguments"/> is <see langword="true"/>.
    /// </remarks>
    public static string MethodArguments { get; } = "Refit.MethodArguments";
}
