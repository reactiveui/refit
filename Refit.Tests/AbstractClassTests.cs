using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Refit;
using RichardSzalay.MockHttp;
using Xunit;

namespace Refit.Tests
{
    public abstract class AbstractApi
    {
        [Get("/item/{id}")]
        protected abstract Task<Item> GetItem(Guid id);

        [Delete("/item/{id}")]
        protected abstract Task<HttpResponseMessage> DeleteItem(Guid id);

        public async Task<bool> DeleteIfExists(Guid id)
        {
            var item = await GetItem(id);
            if (item == null)
                throw new Exception("Item doesn't exists");

            var deleted = await DeleteItem(item.Id);
            return deleted.IsSuccessStatusCode;
        }

        public void DeleteVoid(Guid id)
        {
            DeleteIfExists(id).GetAwaiter().GetResult();
        }

        public Task DeleteVoidAsync(Guid id)
        {
            return DeleteIfExists(id);
        }
    }

    public class Item
    {
        public Guid Id { get; set; }
        public string Value { get; set; }
    }

    
    public class AbstractClassTests
    {
        [Fact]
        public async Task AbstractClassTest1() {

            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings {
                HttpMessageHandlerFactory = () => mockHttp
            };

            var id = Guid.NewGuid();

            mockHttp.Expect(HttpMethod.Get, $"https://example.api.com/item/{id}")
                .Respond(HttpStatusCode.OK, "application/json", $"{{ 'Id':'{id}', 'Value':'This is an existing Item' }}");
                
            mockHttp.Expect(HttpMethod.Delete, $"https://example.api.com/item/{id}")
                .Respond(HttpStatusCode.NoContent);

            var fixture = RestService.For<AbstractApi>("https://example.api.com", settings);
            var deleted = await fixture.DeleteIfExists(id);

            
            Assert.True(deleted);
        }
    }
}

