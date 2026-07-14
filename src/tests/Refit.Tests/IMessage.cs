// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>A marker interface used as a generic constraint by the generator test fixtures.</summary>
[SuppressMessage("Design", "SST1437:Add members to type or remove it", Justification = "Intentional empty marker fixture interface used as a generic constraint for Refit tests.")]
public interface IMessage;
