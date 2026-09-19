// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveUI.Primitives;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Checks awaited results, observable subscriptions and guarded response content.</summary>
internal static class ReturnTypes
{
    /// <summary>Exercises each result shape and verifies requests repeat for observable subscriptions.</summary>
    /// <param name="host">The local handler and generated-metadata client configuration for these samples.</param>
    /// <returns>A task that completes after the sample assertions pass.</returns>
    internal static async Task RunAsync(SampleHost host)
    {
        const int ExpectedRequests = 5;
        const int ExpectedSubscriptions = 2;

        host.Http.Add(new() { Method = HttpMethod.Get, Template = "/person", Reusable = true }, Reply.With(new Person(1, "Ada")));
        host.Http.Add(new() { Method = HttpMethod.Get, Template = "/ping", Reusable = true }, Reply.Text("pong"));

        int before = host.Http.Requests.Count;
        IReturnTypesApi api = RestService.ForGenerated<IReturnTypesApi>(host.Client, host.Settings);

        Person fromTask = await api.GetTaskAsync(CancellationToken.None);
        Person fromValueTask = await api.GetValueTaskAsync(CancellationToken.None);
        Console.WriteLine(fromTask.Name); // Ada
        Console.WriteLine(fromValueTask.Name); // Ada
        await api.PingAsync(CancellationToken.None);

        IObservable<string> names = api.GetPerson(CancellationToken.None)
            .Where(static person => person.Id > 0)
            .Select(static person => person.Name);
        TaskCompletionSource finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using IDisposable subscription = names.Subscribe(
            Console.WriteLine,
            error => finished.TrySetException(error),
            () => finished.TrySetResult());
        await finished.Task;

        using ApiResponse<Person> response = await api.GetResponseAsync(CancellationToken.None);
        await response.EnsureSuccessfulAsync();
        if (response.HasContent)
        {
            Console.WriteLine(response.Content.Name); // Ada
        }

        SampleCheck.Equal(fromTask, fromValueTask);
        SampleCheck.Equal(before + ExpectedRequests, host.Http.Requests.Count);

        before = host.Http.Requests.Count;
        IObservable<Person> signal = api.GetPerson(CancellationToken.None);
        _ = await signal.ToTask();
        _ = await signal.ToTask();

        SampleCheck.Equal(before + ExpectedSubscriptions, host.Http.Requests.Count);
    }
}
