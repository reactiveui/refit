using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refit
{
    public interface IQueryParameters
    {
        IDictionary<string, string> GetParameters();
    }
}
