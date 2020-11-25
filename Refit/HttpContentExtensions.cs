using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Refit
{
#if !NET5_0
    static class HttpContentExtensions
    {

#pragma warning disable IDE0060 // Remove unused parameter
        public static Task<Stream> ReadAsStreamAsync(this HttpContent httpContent, CancellationToken cancellationToken)

        {
            return httpContent.ReadAsStreamAsync();
        }

        public static Task<string> ReadAsStringAsync(this HttpContent httpContent, CancellationToken cancellationToken)
        {
            return httpContent.ReadAsStringAsync();
        }

#pragma warning restore IDE0060 // Remove unused parameter
    }
#endif
}
