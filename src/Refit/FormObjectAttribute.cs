// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// Marks a complex-object parameter of a <c>[Multipart]</c> method so its public properties are written as individual
/// <c>multipart/form-data</c> text parts (one part per property) instead of the default single serialized part.
/// </summary>
/// <remarks>
/// Without this attribute a complex object is added to a multipart request as one serialized part (for example a single
/// <c>application/json</c> body named after the parameter), which server-side form model binding cannot bind field by
/// field. With it, each property becomes its own named form field, so a server model bound from the form (such as an
/// ASP.NET <c>[FromForm]</c> model) receives the individual values.
/// <para>
/// Field names are resolved per property in the same order as url-encoded body flattening: an explicit
/// <see cref="AliasAsAttribute"/>, then <see cref="IHttpContentSerializer.GetFieldNameForProperty"/>, then
/// <see cref="RefitSettings.UrlParameterKeyFormatter"/>. Values are rendered through
/// <see cref="RefitSettings.FormUrlEncodedParameterFormatter"/>, collections honour
/// <see cref="RefitSettings.CollectionFormat"/>, and a nested object composes its children under a
/// <c>parent.child</c> field name (bounded by a nesting-depth cap and a reference-cycle guard).
/// </para>
/// <para>
/// File-typed members (<see cref="System.IO.Stream"/>, <c>byte[]</c>, <see cref="System.IO.FileInfo"/> and the Refit
/// part types) are not converted to file parts by this attribute; pass those as their own separate part parameters.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// interface IUploadApi
/// {
///   [Multipart]
///   [Post("/setup")]
///   Task SetupAsync([FormObject] SetupModel model, [AliasAs("recipe")] StreamPart recipe);
/// }
/// </code>
/// Each public property of <c>SetupModel</c> is sent as its own form field alongside the <c>recipe</c> file part.
/// </example>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FormObjectAttribute : Attribute;
