// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Describes local replies used to check client creation and authorization.</summary>
internal interface IClientApi
{
    /// <summary>Reads the locally supplied person.</summary>
    /// <returns>The person supplied by the sample transport.</returns>
    [Get("/clients/person")]
    Task<Person> ReadAsync();

    /// <summary>Reads a person with a token obtained from settings.</summary>
    /// <returns>The person supplied by the sample transport.</returns>
    [Get("/clients/authorized")]
    [Headers("Authorization: Bearer")]
    Task<Person> AuthorizedAsync();

    /// <summary>Reads a person with the caller's authorization token.</summary>
    /// <param name="token">The token supplied by the caller.</param>
    /// <returns>The person supplied by the sample transport.</returns>
    [Get("/clients/explicit")]
    Task<Person> ExplicitAsync([Authorize] string token);
}
