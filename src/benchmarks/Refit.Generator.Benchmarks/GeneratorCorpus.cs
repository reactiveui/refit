// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text;

namespace Refit.Generator.Benchmarks;

/// <summary>Provides representative Refit interface source corpora used across the generator benchmarks.</summary>
internal static class GeneratorCorpus
{
    /// <summary>The number of interfaces emitted into the large corpus.</summary>
    private const int LargeInterfaceCount = 40;

    /// <summary>The number of methods emitted per interface in the large corpus.</summary>
    private const int LargeMethodsPerInterface = 12;

    /// <summary>The initial builder capacity for the large corpus, sized to hold it without regrowth.</summary>
    private const int LargeCorpusInitialCapacity = 64 * 1024;

    /// <summary>The shared using directives and namespace preamble every corpus source begins with.</summary>
    private const string Preamble =
        """
        using System;
        using System.Collections.Generic;
        using System.Net.Http;
        using System.Threading;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorBench;

        """;

    /// <summary>The per-method source templates cycled across the large corpus to spread it over binding shapes.
    /// Each carries a <c>{route}</c> and <c>{n}</c> placeholder replaced per emission.</summary>
    private static readonly string[] _largeMethodTemplates =
    [
        "    [Get(\"{route}/{id}\")]\n    Task<Entity> Get{n}(int id, CancellationToken token);\n",
        "    [Get(\"{route}\")]\n    Task<List<Entity>> List{n}(int page, int pageSize, string? sort, bool archived);\n",
        "    [Post(\"{route}\")]\n    Task<Entity> Create{n}([Body] Entity entity);\n",
        "    [Put(\"{route}/{id}\")]\n    Task Update{n}(int id, [Body] Entity entity, [Header(\"If-Match\")] string etag);\n",
        "    [Get(\"{route}/search\")]\n    Task<string> Search{n}(string q, [Query(CollectionFormat.Multi)] int[] tags, [AliasAs(\"c\")] string? cursor);\n",
        "    [Delete(\"{route}/{id}\")]\n    Task Delete{n}(int id);\n",
    ];

    /// <summary>Gets a single small CRUD interface: the common "one client, a handful of methods" case.</summary>
    public static string Small { get; } =
        Preamble +
        """
        public sealed class Widget
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        public interface IWidgetApi
        {
            [Post("/widgets")]
            Task<Widget> Create([Body] Widget payload);

            [Get("/widgets")]
            Task<List<Widget>> ReadAll();

            [Get("/widgets/{id}")]
            Task<Widget> ReadOne(int id);

            [Put("/widgets/{id}")]
            Task Update(int id, [Body] Widget payload);

            [Delete("/widgets/{id}")]
            Task Delete(int id);
        }
        """;

    /// <summary>Gets a mid-sized corpus exercising query, path, header, and body binding across several interfaces.</summary>
    public static string Medium { get; } = BuildMedium();

    /// <summary>Gets a large multi-interface, multi-method corpus for cold-run and throughput measurement.</summary>
    public static string Large { get; } = BuildLarge();

    /// <summary>Gets a query-heavy corpus exercising scalar, collection, object, and converter query bindings.</summary>
    public static string QueryHeavy { get; } =
        Preamble +
        """
        using System.Runtime.Serialization;

        public enum BenchSort
        {
            [EnumMember(Value = "date-desc")]
            DateDescending,
            Name,
        }

        public sealed class BenchFilter
        {
            public string? Name { get; set; }
            public int? MinValue { get; set; }
            public BenchSort Sort { get; set; }
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

            [Get("/object")]
            Task<string> ByObject([Query] BenchFilter filter, string tag);

            [Get("/fmt")]
            Task<string> Formatted([Query(Format = "0.00")] double price, [Query(TreatAsString = true)] object raw);
        }
        """;

    /// <summary>Gets a multipart-heavy corpus exercising stream, byte-array, string, and typed multipart parts.</summary>
    public static string MultipartHeavy { get; } =
        Preamble +
        """
        using System.IO;

        public interface IMultipartApi
        {
            [Multipart]
            [Post("/upload/stream")]
            Task<string> UploadStream(Stream file, string description);

            [Multipart]
            [Post("/upload/bytes")]
            Task<string> UploadBytes([AliasAs("blob")] byte[] data, int width, int height);

            [Multipart]
            [Post("/upload/parts")]
            Task<string> UploadParts(string title, string author, StreamPart cover, IEnumerable<StreamPart> pages);

            [Multipart]
            [Post("/upload/typed")]
            Task<string> UploadTyped([AliasAs("meta")] Dictionary<string, string> metadata, ByteArrayPart thumbnail);
        }
        """;

    /// <summary>Gets the corpus source text for a given size.</summary>
    /// <param name="size">The corpus size.</param>
    /// <returns>The corpus source text.</returns>
    public static string SourceFor(CorpusSize size) => size switch
    {
        CorpusSize.Small => Small,
        CorpusSize.Medium => Medium,
        _ => Large,
    };

    /// <summary>Builds a mid-sized corpus with a handful of varied interfaces.</summary>
    /// <returns>The medium corpus source text.</returns>
    private static string BuildMedium()
    {
        var builder = new StringBuilder(Preamble);
        _ = builder.Append(
            """
            public sealed class User
            {
                public int Id { get; set; }
                public string? Name { get; set; }
                public string? Email { get; set; }
            }

            public sealed class Order
            {
                public int Id { get; set; }
                public decimal Total { get; set; }
            }

            public interface IUserApi
            {
                [Get("/users")]
                Task<List<User>> List(int page, int pageSize, string? sort);

                [Get("/users/{id}")]
                Task<User> Get(int id);

                [Post("/users")]
                Task<User> Create([Body] User user);

                [Headers("Accept: application/json")]
                [Put("/users/{id}")]
                Task Update(int id, [Body] User user, [Header("If-Match")] string etag);
            }

            public interface IOrderApi
            {
                [Get("/orders")]
                Task<List<Order>> List([Query] int[] statuses, string? cursor);

                [Get("/orders/{id}")]
                Task<Order> Get(int id, CancellationToken token);

                [Post("/orders/{id}/refund")]
                Task Refund(int id, [Body] Order order);
            }

            public interface ISearchApi
            {
                [Get("/search")]
                Task<string> Query(string q, [AliasAs("f")] string[] filters, bool exact);
            }
            """);
        return builder.ToString();
    }

    /// <summary>Builds a large corpus with many interfaces and methods exercising varied binding shapes.</summary>
    /// <returns>The large corpus source text.</returns>
    private static string BuildLarge()
    {
        var builder = new StringBuilder(LargeCorpusInitialCapacity);
        _ = builder.Append(Preamble);
        _ = builder.Append(
            """
            public sealed class Entity
            {
                public int Id { get; set; }
                public string? Name { get; set; }
                public string? Description { get; set; }
            }

            """);

        for (var i = 0; i < LargeInterfaceCount; i++)
        {
            var route = "/res" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _ = builder.Append("public interface IResource").Append(i).Append("Api\n{\n");
            for (var m = 0; m < LargeMethodsPerInterface; m++)
            {
                var template = _largeMethodTemplates[m % _largeMethodTemplates.Length];
                _ = builder.Append(
                    template
                        .Replace("{route}", route, StringComparison.Ordinal)
                        .Replace("{n}", m.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
            }

            _ = builder.Append("}\n\n");
        }

        return builder.ToString();
    }
}
