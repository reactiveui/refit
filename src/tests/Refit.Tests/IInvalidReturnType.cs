// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Fixture whose method returns a non-task type, used to verify Refit rejects it.</summary>
public interface IInvalidReturnType
{
    /// <summary>Endpoint with an unsupported synchronous return type.</summary>
    /// <returns>The response body.</returns>
    [Get("/")]
    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Refit endpoint method fixture; must remain a method to exercise the generator.")]
    string GetValue();
}
