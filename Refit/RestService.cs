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

    public interface IRestMethodResolver
    {
        Dictionary<string, RestMethodInfo> GetInterfaceRestMethodInfo(Type targetInterface);
    }

    public static class RestService
    {
#if !PORTABLE
        static internal IRestService platformRestService = new CastleRestService();
        public static readonly IRestMethodResolver RestMethodResolver = new DefaultRestMethodResolver();
#else
        static internal IRestService platformRestService;
        public static IRestMethodResolver RestMethodResolver;
        
        static RestService()
        {
            var fullName = typeof(RequestBuilder).AssemblyQualifiedName;
            var builderFactoryToFind = fullName.Replace("RequestBuilder", "CastleRestService").Replace("Refit-Portable", "Refit");

            var builderFactoryType = Type.GetType(builderFactoryToFind);
            platformRestService = Activator.CreateInstance(builderFactoryType) as IRestService;

            var restMethodResolverToFind = fullName.Replace("RequestBuilder", "DefaultRestMethodResolver").Replace("Refit-Portable", "Refit");

            var restMethodResolverType = Type.GetType(restMethodResolverToFind);
            RestMethodResolver = Activator.CreateInstance(restMethodResolverType) as IRestMethodResolver;
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