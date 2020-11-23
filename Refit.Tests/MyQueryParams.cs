using System;
using System.Collections.Generic;
using System.Text;

namespace Refit.Tests
{
    public record MySimpleQueryParams
    {
        public string FirstName { get; set; }

        [AliasAs("lName")]
        public string LastName { get; set; }
    }

    public class MyComplexQueryParams
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        [AliasAs("Addr")]
        public Address Address { get; set; } = new Address();

        public Dictionary<string, object> MetaData { get; set; } = new Dictionary<string, object>();

        public List<object> Other { get; set; } = new List<object>();
    }

    public record Address
    {
        [AliasAs("Zip")]
        public int Postcode { get; set; }
        public string Street { get; set; }
    }
}
