// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Reads people incrementally from JSON arrays, JSON lines or server-sent events.</summary>
internal interface IStreamingApi
{
    /// <summary>Yields people as the response stream is parsed.</summary>
    /// <param name="cancellationToken">The token that cancels request execution and response reading.</param>
    /// <returns>The people parsed incrementally from the response stream.</returns>
    [Get("/people")]
    IAsyncEnumerable<Person> ReadPeopleAsync(CancellationToken cancellationToken);
}
