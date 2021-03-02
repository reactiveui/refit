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

        public CatsService(Uri baseUrl)
        {
            _httpClient = new HttpClient(new HttpClientDiagnosticsHandler(new HttpClientHandler())) { BaseAddress = baseUrl };
            _theCatsApi = RestService.For<ITheCatsAPI>(_httpClient);
        }

        public async Task<IEnumerable<SearchResult>> Search(string breed)
        {
            return await _theCatsApi.Search(breed).ConfigureAwait(false);
        }
    }
}
