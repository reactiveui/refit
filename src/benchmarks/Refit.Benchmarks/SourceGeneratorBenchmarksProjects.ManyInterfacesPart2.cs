// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Benchmarks;

/// <summary>Holds segment 2 of the many-interfaces benchmark source project (interfaces 25-49).</summary>
public static partial class SourceGeneratorBenchmarksProjects
{
    /// <summary>Gets segment 2 of the source concatenated by <see cref="ManyInterfaces"/>.</summary>
    private const string ManyInterfacesPart2 =
        """
        public interface IReallyExcitingCrudApi25<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi26<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi27<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi28<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi29<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi30<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi31<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi32<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi33<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi34<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi35<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi36<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi37<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi38<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi39<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi40<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi41<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi42<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi43<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi44<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi45<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi46<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi47<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi48<T, in TKey> where T : class
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
        public interface IReallyExcitingCrudApi49<T, in TKey> where T : class
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
