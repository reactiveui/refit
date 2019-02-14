using System;
using System.Threading.Tasks;

namespace Refit
{
    /// <summary>
    /// An ApiException that is raised according to RFC 7807, which contains problem details for validation exceptions.
    /// </summary>
    [Serializable]
    public class ValidationApiException : ApiException
    {

        ValidationApiException(ApiException apiException) :
            base(apiException.RequestMessage, apiException.HttpMethod, apiException.StatusCode, apiException.ReasonPhrase, apiException.Headers, apiException.RefitSettings)
        {
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        /// <summary>
        /// Creates a new instance of a ValidationException from an existing ApiException.
        /// </summary>
        /// <param name="exception">An instance of an ApiException to use to build a ValidationException.</param>
        /// <returns>ValidationApiException</returns>
        public static async Task<ValidationApiException> Create(ApiException exception)
#pragma warning restore VSTHRD200
        {
            return new ValidationApiException(exception)
            {
                Content = await exception.GetContentAsAsync<ProblemDetails>().ConfigureAwait(false)
            };
        }

        /// <summary>
        /// The problem details of the RFC 7807 validation exception.
        /// </summary>
        public new ProblemDetails Content { get; private set; }
    }
}
