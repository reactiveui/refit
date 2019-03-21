using HttpClientDiagnostics;
using Meow.Responses;
using Refit;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Meow
{
    public class CatsService
    {
        private readonly HttpClient _httpClient;
        private readonly ITheCatsAPI _theCatsApi;

        public CatsService(string baseUrl)
        {
            _httpClient = new HttpClient(new HttpClientDiagnosticsHandler());
            _theCatsApi = RestService.For<ITheCatsAPI>(baseUrl);
        }

        public async Task<BreedsResponse> Search(string breed)
        {
            return await _theCatsApi.Search(breed).ConfigureAwait(false);
        }
    }
}
