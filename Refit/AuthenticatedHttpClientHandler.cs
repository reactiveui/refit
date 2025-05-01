using System.Net.Http;
using System.Net.Http.Headers;

namespace Refit
{
    class AuthenticatedHttpClientHandler : DelegatingHandler
    {
        readonly Func<HttpRequestMessage, CancellationToken, Task<string>> getToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticatedHttpClientHandler"/> class.
        /// </summary>
        /// <param name="getToken">The function to get the authentication token.</param>
        /// <param name="innerHandler">The optional inner handler.</param>
        /// <exception cref="ArgumentNullException"><paramref name="getToken"/> must not be null.</exception>
        /// <remarks>
        /// Warning: This constructor sets the <see cref="DelegatingHandler.InnerHandler"/> to an instance
        /// of <see cref="HttpClientHandler"/>, when <paramref name="innerHandler"/> is <c>null</c>. This is
        /// a behavior which is incompatible with the <code>IHttpClientBuilder</code>.
        /// </remarks>
        public AuthenticatedHttpClientHandler(
            Func<HttpRequestMessage, CancellationToken, Task<string>> getToken,
            HttpMessageHandler? innerHandler = null
        )
            : base(innerHandler ?? new HttpClientHandler())
        {
            this.getToken = getToken ?? throw new ArgumentNullException(nameof(getToken));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticatedHttpClientHandler"/> class.
        /// </summary>
        /// <param name="innerHandler">The optional inner handler.</param>
        /// <param name="getToken">The function to get the authentication token.</param>
        /// <exception cref="ArgumentNullException"><paramref name="getToken"/> must not be null.</exception>
        /// <remarks>
        /// This function doesn't set the <see cref="DelegatingHandler.InnerHandler"/> automatically to an
        /// instance of <see cref="HttpClientHandler"/> when <paramref name="innerHandler"/> is null,
        /// which is different from the old (legacy) constructor, and compliant with the behavior expected
        /// by the <code>IHttpClientBuilder</code>.
        /// </remarks>
        public AuthenticatedHttpClientHandler(
            HttpMessageHandler? innerHandler,
            Func<HttpRequestMessage, CancellationToken, Task<string>> getToken
        )
        {
            this.getToken = getToken ?? throw new ArgumentNullException(nameof(getToken));
            if (innerHandler != null)
                InnerHandler = innerHandler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            // See if the request has an authorize header
            var auth = request.Headers.Authorization;
            if (auth != null)
            {
                var token = await getToken(request, cancellationToken).ConfigureAwait(false);
                request.Headers.Authorization = new AuthenticationHeaderValue(auth.Scheme, token);
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
