using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



using Refit; // InterfaceStubGenerator looks for this
using Refit.Tests.Api1;



namespace Refit.Tests.Api1
{
    public class TypeConflict
    {
        public string Name { get; set; }
    }
   
}

namespace Refit.Tests
{
    

    public interface IConflictingUser
    {
        [Get("/foo")]
        Task FooBar(TypeConflict user);
    }

}
