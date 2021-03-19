using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Refit; // InterfaceStubGenerator looks for this
using static System.Math; // This is here to verify https://github.com/reactiveui/refit/issues/283

namespace Refit.Tests
{
    public class DataEntity
    {
    }

    public interface IDataApiA : IDataCrudApi<DataEntity>
    {
        [Get("")]
        Task PingA();
    }

    public interface IDataApiB : IDataCrudApi<DataEntity, int>
    {
        [Get("")]
        Task PingB();
    }

    public interface IDataCrudApi<T> : IDataCrudApi<T, long>  where T : class
    {
        [Post("")]
        Task<T> Copy([Body] T payload);
    }

    public interface IDataCrudApi<T, TKey> where T : class
    {
        [Post("")]
        Task<T> Create([Body] T payload);

        [Get("")]
        Task<List<T>> ReadAll<TFoo>() where TFoo : new();

        [Get("")]
        Task<List<T>> ReadAll<TFoo, TBar>() where TFoo : new()
                                            where TBar : struct;

        [Get("/{key}")]
        Task<T> ReadOne(TKey key);

        [Put("/{key}")]
        Task Update(TKey key, [Body]T payload);

        [Delete("/{key}")]
        Task Delete(TKey key);

        [Get("")]
        Task ReadAllClasses<TFoo>()
            where TFoo : class, new();
    }


    public class DatasetQueryItem<TResultRow>
        where TResultRow : class, new()
    {
        [JsonProperty("global_id")]
        public long GlobalId { get; set; }

        public long Number { get; set; }

        [JsonProperty("Cells")]
        public TResultRow Value { get; set; }
    }

    public interface IDataMosApi
    {
        [Get("/datasets/{dataSet}/rows")]
        Task<DatasetQueryItem<TResulRow>[]> GetDataSetItems<TResulRow>()
            where TResulRow : class, new(); 
    }
}
