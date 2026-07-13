// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>One dotted <c>{param.Prop}</c> path placeholder bound to a declared property of the enclosing parameter.</summary>
/// <param name="Location">The placeholder's range within the path template.</param>
/// <param name="PropertyClrName">The member access off the parameter (<c>@param.PropertyClrName</c>); a nested
/// <c>{a.b.c}</c> binding is a null-conditional chain such as <c>b?.c</c>.</param>
/// <param name="TopLevelClrName">The first (top-level) CLR property in the access, excluded from the residual query so a
/// nested binding marks the whole top-level property consumed.</param>
/// <param name="PropertyType">The fully-qualified property type, passed to the URL parameter formatter.</param>
/// <param name="ValueFormat">The reflection-free rendering strategy for the property value.</param>
/// <param name="PropertyCanBeNull">Whether the property value requires a null check before formatting.</param>
internal sealed record PathObjectBindingModel(
    Range Location,
    string PropertyClrName,
    string TopLevelClrName,
    string PropertyType,
    InlineValueFormatModel ValueFormat,
    bool PropertyCanBeNull);
