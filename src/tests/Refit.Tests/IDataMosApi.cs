// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit API that returns generically-typed dataset query rows.</summary>
public interface IDataMosApi
{
    /// <summary>Gets the rows of the named dataset.</summary>
    /// <typeparam name="TResultRow">The type of each returned row value.</typeparam>
    /// <returns>The dataset rows.</returns>
    [Get("/datasets/{dataSet}/rows")]
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters to enable type inference",
        Justification = "Intentional test fixture exercising Refit generation of generic methods with unused type parameters.")]
    Task<DatasetQueryItem<TResultRow>[]> GetDataSetItems<TResultRow>()
        where TResultRow : class, new();
}
