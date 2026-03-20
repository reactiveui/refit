using Meow.Responses;

using Refit;

namespace Meow;

[Headers("x-api-key: b95bfb30-55bc-4327-bb8b-35d740f70051")]
public interface ITheCatsAPI
{
    [Get("/v1/images/search")]
    Task<IEnumerable<SearchResult>> Search([AliasAs("q")] string breedIdentifier);
}
