#if NETSTANDARD1_4
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace System.Web
{
    class HttpUtility
    {
        internal static NameValueCollection ParseQueryString(string v)
        {
            var parsed = QueryHelpers.ParseQuery(v);

            var all = from kvp in parsed
                      from val in kvp.Value
                      select new { kvp.Key, Value = val };

            var nvc = new NameValueCollection();
            foreach(var item in all) {
                nvc.Add(item.Key, item.Value);
            }

            return nvc;
        }

        internal static string UrlEncode(string x) => UrlEncoder.Default.Encode(x);        
    }
}
#endif