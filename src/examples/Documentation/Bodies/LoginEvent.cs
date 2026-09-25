// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>A successful sign-in, one of the derived types <see cref="AuditEvent"/> declares.</summary>
/// <param name="ActorId">The identifier of the account that signed in.</param>
/// <param name="IpAddress">The address the sign-in was made from.</param>
internal sealed record LoginEvent(string ActorId, string IpAddress) : AuditEvent(ActorId);
