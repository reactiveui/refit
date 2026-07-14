// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>Holds the <see cref="RefitSettings"/> associated with a specific Refit interface type.</summary>
/// <typeparam name="T">The Refit interface type the settings belong to.</typeparam>
/// <remarks>Initializes a new instance of the <see cref="SettingsFor{T}"/> class.</remarks>
/// <param name="settings">The settings.</param>
[SuppressMessage(
    "StyleSharp",
    "SST1452:Unused type parameters should be removed",
    Justification = "T is the DI service identity: SettingsFor<T> is registered and resolved by closed generic type to key settings to a Refit interface.")]
public class SettingsFor<T>(RefitSettings? settings) : ISettingsFor
{
    /// <summary>Gets the settings.</summary>
    /// <value>
    /// The settings.
    /// </value>
    public RefitSettings? Settings { get; } = settings;
}
