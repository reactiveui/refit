// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>The naming and query attributes read from a form-body property.</summary>
/// <param name="Alias">The <c>[AliasAs]</c> name, or <see langword="null"/>.</param>
/// <param name="Json">The <c>[JsonPropertyName]</c> name, or <see langword="null"/>.</param>
/// <param name="Newtonsoft">The Newtonsoft.Json <c>[JsonProperty]</c> name, or <see langword="null"/>.</param>
/// <param name="Xml">The <c>[XmlElement]</c> or <c>[XmlAttribute]</c> name, or <see langword="null"/>.</param>
/// <param name="Query">The parsed <c>[Query]</c> data.</param>
internal readonly record struct FormPropertyAttributes(
    string? Alias,
    string? Json,
    string? Newtonsoft,
    string? Xml,
    RequestBodyParsing.QueryFormData Query);
