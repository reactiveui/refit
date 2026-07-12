// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Refit.GeneratedCode.TestModels.Scenarios
{
    /// <summary>Exercises generated Refit code under the repository analyzer configuration.</summary>
    public interface IGeneratedUserApi
    {
        /// <summary>Gets a user.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user payload.</returns>
        [Get("/users")]
        public Task<ApiResponse<string>> GetUserAsync(
            CancellationToken cancellationToken);

        /// <summary>Creates a user.</summary>
        /// <param name="payload">The user payload.</param>
        /// <param name="headers">The request headers.</param>
        /// <returns>The created user payload.</returns>
        [Post("/users")]
        public Task<string> CreateUserAsync(
            [Body] string payload,
            [HeaderCollection] IDictionary<string, string> headers);

        /// <summary>Searches users with generated inline query construction.</summary>
        /// <param name="query">The search text.</param>
        /// <param name="page">The optional page number.</param>
        /// <param name="ids">Identifiers expanded as repeated pairs.</param>
        /// <param name="sort">The compile-time-resolved sort order.</param>
        /// <param name="flag">A valueless query flag.</param>
        /// <param name="cursor">A caller-encoded continuation cursor.</param>
        /// <returns>The matching user payload.</returns>
        [Get("/users/search")]
        public Task<string> SearchUsersAsync(
            [AliasAs("q")] string query,
            int? page,
            [Query(CollectionFormat.Multi)] IReadOnlyList<int> ids,
            GeneratedUserSort sort,
            [QueryName] string flag,
            [Encoded] string cursor);
    }
}
