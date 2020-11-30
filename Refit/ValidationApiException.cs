using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Refit
{
    /// <summary>
    /// An ApiException that is raised according to RFC 7807, which contains problem details for validation exceptions.
    /// </summary>
    [Serializable]
    public class ValidationApiException : ApiException
    {
        static readonly JsonSerializerOptions SerializerOptions = new();

        static ValidationApiException()
        {
            SerializerOptions.PropertyNameCaseInsensitive = true;
            SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            SerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
        } 

        ValidationApiException(ApiException apiException) :
            base(apiException.RequestMessage, apiException.HttpMethod, apiException.Content, apiException.StatusCode, apiException.ReasonPhrase, apiException.Headers, apiException.RefitSettings)
        {
        }

        /// <summary>
        /// Creates a new instance of a ValidationException from an existing ApiException.
        /// </summary>
        /// <param name="exception">An instance of an ApiException to use to build a ValidationException.</param>
        /// <returns>ValidationApiException</returns>
        public static ValidationApiException Create(ApiException exception)
        {
            if (exception is null)
                throw new ArgumentNullException(nameof(exception));
            if (string.IsNullOrWhiteSpace(exception.Content))
                throw new ArgumentException("Content must be an 'application/problem+json' compliant json string.");

            var ex = new ValidationApiException(exception);

            if(!string.IsNullOrWhiteSpace(exception.Content))
            {
                ex.Content = JsonSerializer.Deserialize<ProblemDetails>(exception.Content!, SerializerOptions);
            }

            return ex;
        }

        /// <summary>
        /// The problem details of the RFC 7807 validation exception.
        /// </summary>
        public new ProblemDetails? Content { get; private set; }
    }
}
