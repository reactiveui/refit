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
#if !NET5_0_OR_GREATER
    static class HttpContentExtensions
    {
#pragma warning disable IDE0079 // Remove unnecessary suppression
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
#pragma warning restore IDE0079 // Remove unnecessary suppression
    }
#endif
}
