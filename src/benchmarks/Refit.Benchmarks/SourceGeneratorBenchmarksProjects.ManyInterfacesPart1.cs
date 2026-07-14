// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Benchmarks;

/// <summary>Holds segment 1 of the many-interfaces benchmark source project (interfaces 0-24).</summary>
public static partial class SourceGeneratorBenchmarksProjects
{
    /// <summary>Gets segment 1 of the source concatenated by <see cref="ManyInterfaces"/>.</summary>
    private const string ManyInterfacesPart1 =
        """
        using System;
        using System.Collections.Generic;
        using System.Net.Http;
        using System.Text;
        using System.Threading;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public interface IReallyExcitingCrudApi0<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi1<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi2<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi3<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi4<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi5<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi6<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi7<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi8<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi9<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi10<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi11<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi12<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi13<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi14<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi15<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi16<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi17<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi18<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi19<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi20<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi21<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi22<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi23<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi24<T, in TKey> where T : class
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
}
