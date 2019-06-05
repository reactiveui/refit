using System.Collections.Generic;
using System.Threading.Tasks;
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
        Task<List<T>> ReadAll();

        [Get("/{key}")]
        Task<T> ReadOne(TKey key);

        [Put("/{key}")]
        Task Update(TKey key, [Body]T payload);

        [Delete("/{key}")]
        Task Delete(TKey key);
    }
}
