// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace CollisionA;

/// <summary>Empty response type used to verify Refit resolves same-named types across namespaces.</summary>
[SuppressMessage("Design", "SST1436:Add members to type or remove it", Justification = "Intentional empty fixture used to verify Refit type-name collision handling across namespaces.")]
public class SomeType;
