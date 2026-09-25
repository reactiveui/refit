// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Documentation;

/// <summary>
/// A polymorphic audit event. Not sealed, so an <c>IEnumerable&lt;AuditEvent&gt;</c> or
/// <c>IAsyncEnumerable&lt;AuditEvent&gt;</c> upload writes each element by its runtime type, not by this declared
/// type; the caller's JSON configuration for <see cref="AuditEvent"/> and its derived types is shown here.
/// </summary>
/// <param name="ActorId">The identifier of the account the event happened to.</param>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(LoginEvent), "login")]
internal abstract record AuditEvent(string ActorId);
