using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refit.Tests {
    public class AliasAsResponseTests
    {
        public class TestAliasObject
        {
            [AliasAs("FIELD_WE_SHOULD_SHORTEN_WITH_ALIAS_AS")]
            public string ShortNameForAlias { get; set; }

            [JsonProperty(PropertyName = "FIELD_WE_SHOULD_SHORTEN_WITH_JSON_PROPERTY")]
            public string ShortNameForJsonProperty { get; set; }
        }

        public interface IMyAliasService
        {
            [Get("/testRoute")]
            Task<TestAliasObject> GetTestObject();
        }
    }
}
