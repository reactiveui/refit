// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.TestingFrameworks;

/// <summary>A person, as returned by the little pretend API these tests call.</summary>
/// <param name="Id">The person's id.</param>
/// <param name="Name">The person's name.</param>
public sealed record Person(int Id, string Name);
