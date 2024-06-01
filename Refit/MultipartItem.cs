using System.Net.Http;
using System.Net.Http.Headers;

namespace Refit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultipartItem"/> class.
    /// </summary>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="contentType">Type of the content.</param>
    /// <exception cref="System.ArgumentNullException">fileName</exception>
    public abstract class MultipartItem(string fileName, string? contentType)
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultipartItem"/> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="name">The name.</param>
        public MultipartItem(string fileName, string? contentType, string? name)
            : this(fileName, contentType)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string? Name { get; }

        /// <summary>
        /// Gets the type of the content.
        /// </summary>
        /// <value>
        /// The type of the content.
        /// </value>
        public string? ContentType { get; } = contentType;

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        public string FileName { get; } = fileName ?? throw new ArgumentNullException(nameof(fileName));

        /// <summary>
        /// Converts to content.
        /// </summary>
        /// <returns></returns>
        public HttpContent ToContent()
        {
            var content = CreateContent();
            if (!string.IsNullOrEmpty(ContentType))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
            }

            return content;
        }

        /// <summary>
        /// Creates the content.
        /// </summary>
        /// <returns></returns>
        protected abstract HttpContent CreateContent();
    }

    /// <summary>
    /// Allows the use of a generic <see cref="Stream"/> in a multipart form body.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="StreamPart"/> class.
    /// </remarks>
    /// <param name="value">The value.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="contentType">Type of the content.</param>
    /// <param name="name">The name.</param>
    /// <exception cref="System.ArgumentNullException">value</exception>
    public class StreamPart(
        Stream value,
        string fileName,
        string? contentType = null,
        string? name = null
        ) : MultipartItem(fileName, contentType, name)
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public Stream Value { get; } = value ?? throw new ArgumentNullException(nameof(value));

        /// <summary>
        /// Creates the content.
        /// </summary>
        /// <returns></returns>
        protected override HttpContent CreateContent()
        {
            return new StreamContent(Value);
        }
    }

    /// <summary>
    /// Allows the use of a <see cref="byte"/> array in a multipart form body.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ByteArrayPart"/> class.
    /// </remarks>
    /// <param name="value">The value.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="contentType">Type of the content.</param>
    /// <param name="name">The name.</param>
    /// <exception cref="System.ArgumentNullException">value</exception>
    public class ByteArrayPart(
        byte[] value,
        string fileName,
        string? contentType = null,
        string? name = null
        ) : MultipartItem(fileName, contentType, name)
    {

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public byte[] Value { get; } = value ?? throw new ArgumentNullException(nameof(value));

        /// <summary>
        /// Creates the content.
        /// </summary>
        /// <returns></returns>
        protected override HttpContent CreateContent()
        {
            return new ByteArrayContent(Value);
        }
    }

    /// <summary>
    /// Allows the use of a <see cref="FileInfo"/> object in a multipart form body.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="FileInfoPart"/> class.
    /// </remarks>
    /// <param name="value">The value.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="contentType">Type of the content.</param>
    /// <param name="name">The name.</param>
    /// <exception cref="System.ArgumentNullException">value</exception>
    public class FileInfoPart(
        FileInfo value,
        string fileName,
        string? contentType = null,
        string? name = null
        ) : MultipartItem(fileName, contentType, name)
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public FileInfo Value { get; } = value ?? throw new ArgumentNullException(nameof(value));

        /// <summary>
        /// Creates the content.
        /// </summary>
        /// <returns></returns>
        protected override HttpContent CreateContent()
        {
            return new StreamContent(Value.OpenRead());
        }
    }
}
