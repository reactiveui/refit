#if NETSTANDARD1_4
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.AspNetCore.WebUtilities;

namespace System.Web
{
    class HttpUtility
    {
        internal static NameValueCollection ParseQueryString(string v)
        {
            var parsed = QueryHelpers.ParseQuery(v);

            var all = from kvp in parsed
                      from val in kvp.Value
                      select new
                      {
                          kvp.Key,
                          Value = val
                      };

            var nvc = new NameValueCollection();
            foreach (var item in all)
            {
                nvc.Add(item.Key, item.Value);
            }

            return nvc;
        }
    }
}
#endif
