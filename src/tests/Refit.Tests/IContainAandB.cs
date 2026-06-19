// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>A Refit interface that only aggregates two base interfaces and declares no members of its own.</summary>
public interface IContainAandB : IAmInterfaceB, IAmInterfaceA;
