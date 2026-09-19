// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>The shared person body used by request, serializer and native examples.</summary>
/// <param name="Id">The person's route identifier.</param>
/// <param name="Name">The person's display name.</param>
internal sealed record Person(int Id, string Name);
