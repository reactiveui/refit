// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Shows the protected route setter available to an HTTP method attribute subclass.</summary>
/// <param name="path">The route stored by the base attribute.</param>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class SearchAttribute(string path) : HttpMethodAttribute(path)
{
    /// <summary>Gets the custom HTTP method.</summary>
    public override HttpMethod Method => new("SEARCH");

    /// <summary>Replaces the stored route before the attribute is used.</summary>
    /// <param name="path">The replacement route.</param>
    internal void ChangePath(string path) => Path = path;
}
