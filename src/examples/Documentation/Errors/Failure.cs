// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Describes the service's rejected-input response.</summary>
/// <param name="Code">The machine-readable failure code.</param>
/// <param name="Message">The explanation for the caller.</param>
internal sealed record Failure(string Code, string Message);
