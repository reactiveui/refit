using System;

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

        /// <summary>
        /// Creates a new instance of a ValidationException from an existing ApiException.
        /// </summary>
        /// <param name="exception">An instance of an ApiException to use to build a ValidationException.</param>
        /// <returns>ValidationApiException</returns>
        public static ValidationApiException Create(ApiException exception)
        {
            return new ValidationApiException(exception);
        }

        /// <summary>
        /// The problem details of the RFC 7807 validation exception.
        /// </summary>
        public new ProblemDetails Content => GetContentAs<ProblemDetails>();

    }
}
