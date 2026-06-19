// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests.ModelNamespace;

/// <summary>Empty response type used to verify Refit handling of reduced usings inside a namespace.</summary>
[SuppressMessage("Design", "SST1436:Add members to type or remove it", Justification = "Intentional empty fixture response type used to verify Refit reduced-using handling.")]
public class SomeType;
