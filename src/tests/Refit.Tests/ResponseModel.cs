// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests.SeparateNamespaceWithModel;

/// <summary>An intentionally empty response model in a separate namespace, used to verify the generator emits the required using directive.</summary>
[SuppressMessage("Design", "SST1436", Justification = "Intentional empty fixture model used to verify the generator emits the required using directive for a separate namespace.")]
public class ResponseModel;
