using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refit.Tests {
    public class AliasAsResponseTests
    {
        public interface IMyAliasService
        {
            [Get("/testRoute")]
            Task<TestAliasObject> GetTestObject();
        }
    }
}
