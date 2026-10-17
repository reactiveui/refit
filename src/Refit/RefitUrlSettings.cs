// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Configures URL formatting and resolution for a Refit client.</summary>
[System.Diagnostics.DebuggerDisplay("RefitUrlSettings")]
public sealed class RefitUrlSettings
{
    /// <summary>
    /// Gets or sets the <see cref="IUrlParameterKeyFormatter"/> instance to use for formatting URL parameter keys
    /// (defaults to <see cref="DefaultUrlParameterKeyFormatter" />). Allows customization of key naming conventions.
    /// </summary>
    public IUrlParameterKeyFormatter UrlParameterKeyFormatter { get; set; } = new DefaultUrlParameterKeyFormatter();

    /// <summary>
    /// Gets or sets a value indicating whether a query object's flattened property keys honor the content serializer's
    /// property name (for example <c>[JsonPropertyName]</c> with the default System.Text.Json serializer, or
    /// <c>[JsonProperty]</c> with Refit.Newtonsoft.Json), matching how form-encoded field names are resolved
    /// (defaults to <see langword="true"/>).
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, flattened query keys use only <c>[AliasAs]</c> and the
    /// <see cref="UrlParameterKeyFormatter"/> over the CLR property name, as in Refit 13 and earlier. <c>[AliasAs]</c>
    /// always takes precedence regardless of this setting.
    /// </remarks>
    public bool HonorContentSerializerPropertyNamesInQuery { get; set; } = true;

    /// <summary>Gets or sets the <see cref="IUrlParameterFormatter"/> instance to use (defaults to <see cref="DefaultUrlParameterFormatter"/>).</summary>
    public IUrlParameterFormatter UrlParameterFormatter { get; set; } = new DefaultUrlParameterFormatter();

    /// <summary>Gets a registry of <see cref="IUrlParameterFormatter"/> instances keyed by the CLR type they format,
    /// consulted before <see cref="UrlParameterFormatter"/> when rendering a value into a path or query string.</summary>
    /// <remarks>
    /// When a value is rendered into a URL (a path parameter, a round-trip path segment, or a query value), its runtime
    /// type (<see cref="object.GetType"/>) is looked up here first; a registered formatter is used for that value, and
    /// any other type falls back to <see cref="UrlParameterFormatter"/>. Matching is by exact runtime type only - no base
    /// class or interface walking - so register the concrete type the value will have at runtime. Both the reflection and
    /// source-generated request builders consult this registry identically. It does not affect header or body
    /// serialization. Registering any entry opts a request out of the generator's inline formatting fast path.
    /// </remarks>
    public Dictionary<Type, IUrlParameterFormatter> UrlParameterFormatterMap { get; } = [];

    /// <summary>Gets or sets the default collection format to use. (defaults to <see cref="CollectionFormat.RefitParameterFormatter"/>).</summary>
    public CollectionFormat CollectionFormat { get; set; } = CollectionFormat.RefitParameterFormatter;

    /// <summary>
    /// Gets or sets a value indicating whether a route placeholder with no matching method argument is allowed.
    /// When false (the default) Refit throws while building the method; when true the unmatched <c>{token}</c> is
    /// left in the URL verbatim so it can be rewritten later, for example inside a <see cref="DelegatingHandler"/>.
    /// </summary>
    public bool AllowUnmatchedRouteParameters { get; set; }

    /// <summary>
    /// Gets or sets how the client's base address is combined with a method's relative URL
    /// (defaults to <see cref="UrlResolutionMode.RefitLegacy"/>). Set to <see cref="UrlResolutionMode.Rfc3986"/>
    /// to opt in to RFC 3986 / <see cref="System.Net.Http.HttpClient"/> style resolution.
    /// </summary>
    public UrlResolutionMode UrlResolution { get; set; } = UrlResolutionMode.RefitLegacy;
}
