// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>The generated backing-field names an interface implementation stores its collaborators in.</summary>
/// <param name="RequestBuilder">The generated field name that stores the request builder.</param>
/// <param name="Settings">The generated field name that stores Refit settings.</param>
internal readonly record struct GeneratedFieldNames(string RequestBuilder, string Settings);
