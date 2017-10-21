using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Refit; // InterfaceStubGenerator looks for this

namespace Refit.Tests
{
    public interface DefaultOn404Api
    {
        [DefaultOn404]
        [Get("/object")]
        Task<object> GetObjectAsync();

        [DefaultOn404]
        [Get("/int")]
        Task<int> GetInt32Async();
    }
}
