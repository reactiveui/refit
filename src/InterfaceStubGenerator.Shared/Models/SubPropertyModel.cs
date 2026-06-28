// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

internal sealed record SubPropertyModel(
    string LowerCaseAccessName,
    string AccessExpression,
    string ParameterType,
    string Property);
