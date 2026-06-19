// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A record that derives from <see cref="BaseRecord"/> and adds its own property.</summary>
/// <param name="Name">The additional name property exposed by the derived record.</param>
public sealed record DerivedRecordWithProperty(string Name) : BaseRecord("value");
