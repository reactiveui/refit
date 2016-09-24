using System.Collections.Generic;

namespace Refit
{
    public interface IQueryParameters
    {
        IDictionary<string, string> GetParameters();
    }
}
