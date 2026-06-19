// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit;

/// <summary>Provides access to the <see cref="RefitSettings"/> for a Refit interface.</summary>
public interface ISettingsFor
{
    /// <summary>Gets the settings.</summary>
    /// <value>
    /// The settings.
    /// </value>
    RefitSettings? Settings { get; }
}
