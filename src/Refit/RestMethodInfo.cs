// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit;

/// <summary>Describes a single Refit REST method and the metadata used to build its request.</summary>
/// <param name="Name">The name of the method.</param>
/// <param name="HostingType">The interface type that declares the method.</param>
/// <param name="MethodInfo">The reflected method information.</param>
/// <param name="RelativePath">The relative URL path template for the method.</param>
/// <param name="ReturnType">The declared return type of the method.</param>
public sealed record RestMethodInfo(
    string Name,
    Type HostingType,
    MethodInfo MethodInfo,
    string RelativePath,
    Type ReturnType);
