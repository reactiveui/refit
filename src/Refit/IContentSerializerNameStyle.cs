// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// Implemented by a content serializer whose property names come from a known attribute family, so generated
/// code can pick the right compile-time name for a form field without reflection.
/// </summary>
public interface IContentSerializerNameStyle
{
    /// <summary>Gets the attribute family this serializer reads property names from.</summary>
    ContentSerializerNameStyle NameStyle { get; }
}
