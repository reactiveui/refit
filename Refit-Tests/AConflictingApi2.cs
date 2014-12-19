using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



using Refit; // InterfaceStubGenerator looks for this
using Refit.Tests.Api2;



namespace Refit.Tests.Api2
{
    public class TypeConflict
    {
        public string Name { get; set; }
    }

}

namespace Refit.Tests
{


    public interface IAnotherApi
    {
        [Get("/foo")]
        Task FooBar(TypeConflict user);
    }

}