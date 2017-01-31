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
        public MultipartItem()
        {

        }

        public MultipartItem(string fileName, string contentType)
        {
            this.FileName = fileName;
            this.ContentType = contentType;
        }

        public virtual string FileName { get; set; }
        public virtual string ContentType { get; set; }

        public virtual HttpContent ToContent()
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
            Value = value;
        }

        public StreamPart(Stream value)
        {
            Value = value;
        }

        public Stream Value { get; set; }

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
            Value = value;
        }

        public ByteArrayPart(byte[] value)
        {
            Value = value;
        }

        public byte[] Value;

        protected override HttpContent CreateContent()
        {
            return new ByteArrayContent(Value);
        }
    }

#if !NETFX_CORE
    public class FileInfoPart : MultipartItem
    {
        public FileInfoPart(FileInfo value, string fileName, string contentType) :
            base(fileName, contentType)
        {
            Value = value;
        }

        public FileInfoPart(FileInfo value)
        {
            Value = value;
        }

        public FileInfo Value { get; set; }

        protected override HttpContent CreateContent()
        {
            return new StreamContent(Value.OpenRead());
        }
    }
#endif
}
