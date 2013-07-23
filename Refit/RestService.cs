using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Refit
{
    interface IRestService
    {
        T For<T>(HttpClient client);
    }

    public static class RestService
    {
#if !PORTABLE
        static internal IRestService platformRestService = new CastleRestService();
#else
        static internal IRestService platformRestService;
        
        static RestService()
        {
            var fullName = typeof(RequestBuilder).AssemblyQualifiedName;
            var toFind = fullName.Replace("RequestBuilder", "CastleRestService").Replace("Refit-Portable", "Refit");
            
            var type = Type.GetType(toFind);
            platformRestService = Activator.CreateInstance(type) as IRestService;
        }
#endif

        public static T For<T>(HttpClient client)
        {
            return platformRestService.For<T>(client);
        }

        public static T For<T>(string hostUrl)
        {
            var client = new HttpClient() { BaseAddress = new Uri(hostUrl) };
            return RestService.For<T>(client);
        }
    }
}