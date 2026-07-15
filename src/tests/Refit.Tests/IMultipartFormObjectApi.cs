// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>Refit API surface exercised by the multipart <see cref="FormObjectAttribute"/> flattening tests.</summary>
public interface IMultipartFormObjectApi
{
    /// <summary>Uploads a model whose properties are flattened into individual form-data parts.</summary>
    /// <param name="model">The model whose properties become form fields.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadFormObject([FormObject] FormObjectUploadModel model);

    /// <summary>Uploads a flattened model alongside a separate file part.</summary>
    /// <param name="model">The model whose properties become form fields.</param>
    /// <param name="recipe">The file part uploaded next to the flattened fields.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadFormObjectWithFile(
        [FormObject] FormObjectUploadModel model,
        [AliasAs("recipe")] StreamPart recipe);

    /// <summary>Uploads a model whose nested object is flattened into composed <c>parent.child</c> fields.</summary>
    /// <param name="model">The nested model to flatten.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadNestedFormObject([FormObject] NestedFormObjectUploadModel model);

    /// <summary>Uploads a model whose field names come from the content serializer.</summary>
    /// <param name="model">The model whose serializer-named property becomes a form field.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadSerializerNamedFormObject([FormObject] SerializerNamedFormObjectModel model);

    /// <summary>Uploads a dictionary flattened into individual form-data parts.</summary>
    /// <param name="fields">The dictionary whose entries become form fields.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadFormDictionary([FormObject] Dictionary<string, string> fields);
}
