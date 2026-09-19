// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Exposes values retained by the example's registered implementation.</summary>
internal interface IRegisteredClient
{
    /// <summary>Gets the settings supplied to the registered factory.</summary>
    RefitSettings Settings { get; }

    /// <summary>Gets the base address supplied to the registered factory.</summary>
    Uri? BaseAddress { get; }
}
