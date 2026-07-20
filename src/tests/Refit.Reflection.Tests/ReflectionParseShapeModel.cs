// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>A request object supplying a nested property chain for path binding and remaining properties for the query.</summary>
public sealed class ReflectionParseShapeModel
{
    /// <summary>Gets or sets the scalar identifier flattened into the query.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the nested object whose <see cref="ReflectionCachingInnerModel.Code"/> binds the path chain.</summary>
    public ReflectionCachingInnerModel? Inner { get; set; }
}
