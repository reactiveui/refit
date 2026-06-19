// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A record whose property is used as a URL parameter source.</summary>
/// <param name="PropValue">The value exposed as a property for URL formatting.</param>
public sealed record MyParams(string PropValue);
