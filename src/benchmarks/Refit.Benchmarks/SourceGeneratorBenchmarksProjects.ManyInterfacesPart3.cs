// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Benchmarks;

/// <summary>Holds segment 3 of the many-interfaces benchmark source project (interfaces 50-74).</summary>
public static partial class SourceGeneratorBenchmarksProjects
{
    /// <summary>Gets segment 3 of the source concatenated by <see cref="ManyInterfaces"/>.</summary>
    private const string ManyInterfacesPart3 =
        """
        public interface IReallyExcitingCrudApi50<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi51<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi52<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi53<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi54<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi55<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi56<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi57<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi58<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi59<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi60<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi61<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi62<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi63<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi64<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi65<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi66<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi67<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi68<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi69<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi70<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi71<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi72<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi73<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi74<T, in TKey> where T : class
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
