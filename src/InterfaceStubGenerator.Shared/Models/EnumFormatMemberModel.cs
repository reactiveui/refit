// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>One enum member resolved at compile time for generated value formatting.</summary>
/// <param name="MemberName">The declared enum member name.</param>
/// <param name="EnumMemberValue">The <c>[EnumMember(Value = ...)]</c> override honored by the default formatter, or null.</param>
internal sealed record EnumFormatMemberModel(string MemberName, string? EnumMemberValue);
