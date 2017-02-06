using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Refit
{
    public abstract class MultipartItem
    {
        public MultipartItem(string fileName, string contentType)
        {
            this.FileName = fileName;
            this.ContentType = contentType;
        }

        public string FileName { get; private set; }
        public string ContentType { get; private set; }

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
        public StreamPart(Stream value, string fileName, string contentType) :
            base(fileName, contentType)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            Value = value;
        }

        public StreamPart(Stream value) :
            this(value, null, null)
        {
        }

        public Stream Value { get; private set; }

        protected override HttpContent CreateContent()
        {
            return new StreamContent(Value);
        }
    }

    public class ByteArrayPart : MultipartItem
    {
        public ByteArrayPart(byte[] value, string fileName, string contentType) :
            base(fileName, contentType)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            Value = value;
        }

        public ByteArrayPart(byte[] value) :
            this(value, null, null)
        {
        }

        public byte[] Value { get; private set; }

        protected override HttpContent CreateContent()
        {
            return new ByteArrayContent(Value);
        }
    }

#if !NETFX_CORE
    public class FileInfoPart : MultipartItem
    {
        public FileInfoPart(FileInfo value, string fileName, string contentType) :
            base(string.IsNullOrEmpty(fileName) && value != null ? value.Name : fileName, contentType)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            Value = value;
        }

        public FileInfoPart(FileInfo value) :
            this(value, null, null)
        {
        }

        public FileInfo Value { get; private set; }

        protected override HttpContent CreateContent()
        {
            return new StreamContent(Value.OpenRead());
        }
    }
#endif
}
