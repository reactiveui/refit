// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Generated HTTP calls exercised by the independent testing handler.</summary>
internal interface ITestingApi
{
    /// <summary>Reads one person's body.</summary>
    /// <param name="id">The person's route identifier.</param>
    /// <returns>The person supplied by the matching stub.</returns>
    [Get("/people/{id}")]
    Task<TestingPerson> GetAsync(int id);

    /// <summary>Sends a person body for typed request inspection.</summary>
    /// <param name="person">The body serialized by the configured JSON context.</param>
    /// <returns>The person supplied by the matching stub.</returns>
    [Post("/people")]
    Task<TestingPerson> CreateAsync([Body] TestingPerson person);
}
