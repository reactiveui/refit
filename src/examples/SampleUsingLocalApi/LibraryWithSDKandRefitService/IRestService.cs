using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;

namespace LibraryWithSDKandRefitService
{
    public interface IRestService
    {
        [Get("/api/values")]
        Task<string> GetWithNoParameter();

        [Get("/api/values/{id}")]
        Task<string> GetWithParameter([AliasAs("id")] int id);

        [Post("/api/values")]
        Task<string> PostWithTestObject([Body] ModelForTest modelObject);

        [Put("/api/values/{id}")]
        Task<string> PutWithParameters([AliasAs("id")] int id, [Body] ModelForTest modelObject);

        [Delete("/api/values/{id}")]
        Task<string> DeleteWithParameters([AliasAs("id")] int id);
    }
}
