// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests.Tcp;

/// <summary>
/// A TCP client fixture used to verify that <see cref="T:Refit.UniqueName"/> produces a name
/// distinct from the identically named client in the <see cref="Refit.Tests.Http"/> namespace.
/// </summary>
[SuppressMessage("Design", "SST1436:Add members to type or remove it", Justification = "Intentional empty type marker; used only as a generic type argument to UniqueName.ForType.")]
public sealed class Client;
