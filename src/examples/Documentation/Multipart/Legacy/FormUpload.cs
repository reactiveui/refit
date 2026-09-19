// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Documentation;

/// <summary>Checks FormObject property flattening in an explicitly JIT-only executable.</summary>
internal static class FormUpload
{
    /// <summary>Executes a reflected form upload and verifies aliases, repeated fields, nulls and file separation.</summary>
    /// <returns>A task that completes after every flattened-field assertion passes.</returns>
    [RequiresUnreferencedCode("FormObject flattens public model properties through the opt-in reflection request builder.")]
    [RequiresDynamicCode("The opt-in reflection request builder closes generic delegates at runtime.")]
    internal static async Task RunAsync()
    {
        using MultipartHandler handler = new();
        using HttpClient client = CreateClient(handler);
        RefitSettings settings = new(new SystemTextJsonContentSerializer(MultipartJsonContext.Default.Options));
        IFormUploadApi api = RestService.For<IFormUploadApi>(client, settings);
        using HttpResponseMessage reply = await api.UploadAsync(new(), new("recipe"u8.ToArray(), "recipe.txt"));
        SampleCheck.Equal("caption", handler.Parts[0].Name);
        SampleCheck.Equal("Annual report", handler.Parts[0].Body);
        SampleCheck.Equal("Tags", handler.Parts[1].Name);
        SampleCheck.Equal("math", handler.Parts[1].Body);
        SampleCheck.Equal("Tags", handler.Parts[2].Name);
        SampleCheck.Equal("code", handler.Parts[2].Body);
        SampleCheck.Equal("Note", handler.Parts[3].Name);
        SampleCheck.Equal(string.Empty, handler.Parts[3].Body);
        SampleCheck.Equal("recipe", handler.Parts[4].Name);
        SampleCheck.Equal("recipe.txt", handler.Parts[4].FileName);
        SampleCheck.Equal(true, new FormObjectAttribute() is Attribute);
    }

    /// <summary>Creates one local client without taking ownership of the separately scoped handler.</summary>
    /// <param name="handler">The independently owned handler that captures text parts.</param>
    /// <returns>The local client, which the caller must dispose.</returns>
    private static HttpClient CreateClient(MultipartHandler handler) => new(handler, disposeHandler: false) { BaseAddress = new("https://uploads.example") };
}
