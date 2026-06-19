// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using Refit;
using Refit.NativeAotSmoke;

const int ExpectedTodoId = 42;

var handler = new NativeAotSmokeHandler();

using var client = new HttpClient(handler) { BaseAddress = new("https://aot.refit.test") };

var jsonOptions = new JsonSerializerOptions(AotJsonContext.Default.Options)
{
    TypeInfoResolver = AotJsonContext.Default,
};

var api = SmokeApiFactory.Create(client, jsonOptions);

var created = await api.CreateTodoAsync(new("prove native aot")).ConfigureAwait(false);

if (created.Id != ExpectedTodoId || created.Title != "prove native aot")
{
    throw new InvalidOperationException("The AOT POST response was not deserialized correctly.");
}

var status = await api.GetStatusAsync().ConfigureAwait(false);

if (!status.IsSuccessStatusCode || status.Content?.Name != "native-aot")
{
    throw new InvalidOperationException("The AOT ApiResponse<T> result was not deserialized correctly.");
}

if (!handler.SawPostBody)
{
    throw new InvalidOperationException("The AOT request body was not serialized through Refit.");
}

Console.WriteLine("Native AOT Refit smoke test passed.");
