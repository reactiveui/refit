// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using Refit;
using Refit.NativeAotSmoke;

const int ExpectedTodoId = 42;

const int FormCount = 2;

const int SearchPage = 3;

const int SecondSearchId = 2;

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

var formResponse = await api.SubmitFormAsync(new("Ada", FormCount)).ConfigureAwait(false);

if (formResponse != "accepted")
{
    throw new InvalidOperationException("The AOT form response was not returned correctly.");
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

// A generic method closed over a concrete type: generated inline (generic JSON body + SendAsync<T, TBody>), no reflection.
var echoed = await api.EchoAsync<Todo>(new("generic inline")).ConfigureAwait(false);

if (echoed.Id != ExpectedTodoId || echoed.Title != "generic inline")
{
    throw new InvalidOperationException("The AOT generic method result was not deserialized correctly.");
}

var searched = await api
    .SearchAsync("a b", SearchPage, [1, SecondSearchId], SmokeSort.DateDescending, "ready", "x%2Fy")
    .ConfigureAwait(false);

if (searched.Name != "native-aot" || !handler.SawExpectedQuery)
{
    throw new InvalidOperationException("The AOT generated query string was not constructed correctly.");
}

if (!handler.SawFormBody)
{
    throw new InvalidOperationException("The AOT URL-encoded request body was not serialized through generated Refit code.");
}

Console.WriteLine("Native AOT Refit smoke test passed.");
