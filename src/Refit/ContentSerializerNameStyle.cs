// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Identifies which serializer attribute family names a form field when the configured content serializer is asked.</summary>
public enum ContentSerializerNameStyle
{
    /// <summary>Field names come from <c>[JsonPropertyName]</c>.</summary>
    SystemTextJson = 0,

    /// <summary>Field names come from Newtonsoft.Json's <c>[JsonProperty]</c>.</summary>
    Newtonsoft = 1,

    /// <summary>Field names come from <c>[XmlElement]</c> or <c>[XmlAttribute]</c>.</summary>
    Xml = 2,
}
