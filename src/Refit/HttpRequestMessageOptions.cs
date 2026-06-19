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
}
