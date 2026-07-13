// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Benchmarks;

/// <summary>Provides sample Refit interface source projects used by the source generator benchmarks.</summary>
public static partial class SourceGeneratorBenchmarksProjects
{
    /// <summary>Gets the source text for a single small Refit interface used by benchmarks.</summary>
    public static string SmallInterface =>
        """
        using System;
        using System.Collections.Generic;
        using System.Net.Http;
        using System.Text;
        using System.Threading;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public interface IReallyExcitingCrudApi<T, in TKey> where T : class
        {
            [Post("")]
            Task<T> Create([Body] T payload);

            [Get("")]
            Task<List<T>> ReadAll();

            [Get("/{key}")]
            Task<T> ReadOne(TKey key);

            [Put("/{key}")]
            Task Update(TKey key, [Body]T payload);

            [Delete("/{key}")]
            Task Delete(TKey key);
        }
        """;

    /// <summary>Gets the source text for a query-heavy Refit interface exercising inline query classification.</summary>
    public static string QueryHeavyInterface =>
        """
        using System;
        using System.Collections.Generic;
        using System.Runtime.Serialization;
        using System.Threading;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public enum BenchSort
        {
            [EnumMember(Value = "date-desc")]
            DateDescending,
            Name,
        }

        public sealed class BenchPayload
        {
            public string? Name { get; set; }
        }

        public interface IQueryHeavyApi
        {
            [Get("/search")]
            Task<string> Search(string q, int? page, int size, bool archived, BenchSort sort);

            [Get("/csv")]
            Task<string> Csv([Query(CollectionFormat.Csv)] int[] ids, [AliasAs("t")] string tag);

            [Get("/multi")]
            Task<string> Multi([Query(CollectionFormat.Multi)] IReadOnlyList<Guid> ids);

            [Get("/flags")]
            Task<string> Flags([QueryName] string[] flags, [Encoded] string cursor);

            [Post("/create")]
            Task<string> Create(BenchPayload payload, string tag, CancellationToken token);

            [Get("/fmt")]
            Task<string> Formatted([Query(Format = "0.00")] double price, [Query(TreatAsString = true)] object raw);
        }
        """;

    /// <summary>Gets the source text containing many Refit interfaces used by larger benchmarks.</summary>
    public static string ManyInterfaces =>
        ManyInterfacesPart1 + ManyInterfacesPart2 + ManyInterfacesPart3 + ManyInterfacesPart4;
}
