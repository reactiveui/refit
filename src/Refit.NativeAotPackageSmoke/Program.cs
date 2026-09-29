// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit.NativeAotPackageSmoke;

(string Name, Func<Task> Run)[] scenarios =
[
    ("generated-only dependency injection", Scenarios.DependencyInjectionAsync),
    ("keyed dependency injection", Scenarios.KeyedDependencyInjectionAsync),
    ("custom query converter", Scenarios.QueryConverterAsync),
    ("multipart upload", Scenarios.MultipartAsync),
    ("typed error body", Scenarios.ErrorBodyAsync),
    ("return type adapter", Scenarios.ReturnTypeAdapterAsync),
    ("streamed JSON array, JSON Lines and server-sent events", Scenarios.StreamedRepliesAsync),
    ("early disposal of a streamed reply", Scenarios.EarlyDisposalAsync),
    ("cancellation of a stalled streamed read", Scenarios.CancellationAsync),
    ("JSON Lines upload", Scenarios.JsonLinesUploadAsync),
    ("cursor paging", Scenarios.PagingAsync),
    ("reply type missing from the JSON context", Scenarios.MissingMetadataAsync),
];

var failures = 0;
foreach (var (name, run) in scenarios)
{
    try
    {
        await run().ConfigureAwait(false);
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL {name}: {ex}");
    }
}

Console.WriteLine($"Native AOT package consumer: {scenarios.Length - failures} of {scenarios.Length} scenarios passed.");
return failures == 0 ? 0 : 1;
