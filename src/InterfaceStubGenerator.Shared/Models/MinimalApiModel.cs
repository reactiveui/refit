// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Describes source-generated Minimal API endpoint generation for a Refit interface.</summary>
/// <param name="JsonSerializerContextType">The JSON source generation context type used by generated handlers.</param>
/// <param name="GenerateClient">Whether the Refit client stub should also be generated.</param>
internal sealed record MinimalApiModel(string JsonSerializerContextType, bool GenerateClient);
