using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Refit
{
    public abstract class MultipartItem
    {
        public MultipartItem(string fileName, string? contentType)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            ContentType = contentType;
        }

        public MultipartItem(string fileName, string? contentType, string? name) : this(fileName, contentType)
        {
            Name = name;
        }

        public string? Name { get; }

        public string? ContentType { get; }

        public string FileName { get; }

        public HttpContent ToContent()
        {
            var content = CreateContent();
            if (!string.IsNullOrEmpty(ContentType))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
            }

            return content;
        }

        protected abstract HttpContent CreateContent();
    }

    public class StreamPart : MultipartItem
    {
        public StreamPart(Stream value, string fileName, string? contentType = null, string? name = null) :
            base(fileName, contentType, name)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public Stream Value { get; }

        protected override HttpContent CreateContent()
        {
            return new StreamContent(Value);
        }
    }

    public class ByteArrayPart : MultipartItem
    {
        public ByteArrayPart(byte[] value, string fileName, string? contentType = null, string? name = null) :
            base(fileName, contentType, name)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public byte[] Value { get; }

        protected override HttpContent CreateContent()
        {
            return new ByteArrayContent(Value);
        }
    }

    public class FileInfoPart : MultipartItem
    {
        public FileInfoPart(FileInfo value, string fileName, string? contentType = null, string? name = null) :
            base(fileName, contentType, name)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public FileInfo Value { get; }

        protected override HttpContent CreateContent()
        {
            return new StreamContent(Value.OpenRead());
        }
    }
}
