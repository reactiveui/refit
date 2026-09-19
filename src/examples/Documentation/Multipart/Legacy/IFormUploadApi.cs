// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Declares a multipart method that requires property flattening through reflection.</summary>
internal interface IFormUploadApi
{
    /// <summary>Flattens model properties into text fields beside a separate file part.</summary>
    /// <param name="fields">The object whose public properties become text parts.</param>
    /// <param name="recipe">The file supplied separately from the flattened object.</param>
    /// <returns>The reply, which the caller must dispose.</returns>
    [Multipart]
    [Post("/form")]
    Task<HttpResponseMessage> UploadAsync([FormObject] FormFields fields, ByteArrayPart recipe);
}
