// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace System.Collections.Specialized;

/// <summary>A minimal string-keyed collection that mirrors the shape of the framework name/value collection.</summary>
[Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal class NameValueCollection : Dictionary<string, string>
{
    /// <summary>Gets all of the keys currently stored in the collection.</summary>
    internal string[] AllKeys => [.. Keys];
}
