// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Paging;

/// <summary>Lists Microsoft Graph users, which pages by following the <c>@odata.nextLink</c> the server returns.</summary>
internal interface IGraphApi
{
    /// <summary>Lists users.</summary>
    /// <param name="top">The most users on a page.</param>
    /// <returns>A lazy sequence of users; a link is followed only when it points at the client's own origin.</returns>
    [Get("/v1.0/users")]
    [Paged(Next = nameof(GraphUserPage.NextLink), SameOrigin = true)]
    PagedEnumerable<GraphUserPage, GraphUser> ListUsers([AliasAs("$top")] int top);
}
