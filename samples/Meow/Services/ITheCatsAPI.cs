using Meow.Responses;
using Refit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Meow
{
    [Headers("x-api-key: redacted")]

    public interface ITheCatsAPI
    {
        [Get("/v1/images/search")]
        Task<BreedsResponse> Search([AliasAs("breed_id")] string breedIdentifier);
    }
}
