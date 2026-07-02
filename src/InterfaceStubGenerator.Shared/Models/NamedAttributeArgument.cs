// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>A named attribute argument assignment already rendered as source expressions.</summary>
/// <param name="Name">The argument name.</param>
/// <param name="ValueExpression">The argument value expression.</param>
internal sealed record NamedAttributeArgument(string Name, string ValueExpression);
