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
    static class HttpContentExtensions
    {
#if !NET5_0
        public static Task<Stream> ReadAsStreamAsync(this HttpContent httpContent, CancellationToken cancellationToken)
        {
            return httpContent.ReadAsStreamAsync();
        }

        public static Task<string> ReadAsStringAsync(this HttpContent httpContent, CancellationToken cancellationToken)
        {
            return httpContent.ReadAsStringAsync();
        }
#endif
    }
}
