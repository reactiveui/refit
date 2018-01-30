using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit
{
    public abstract class HttpMethodAttribute : Attribute
    {
        protected string path;

        public HttpMethodAttribute(string path)
        {
            Path = path;
        }

        public abstract HttpMethod Method { get; }

        public virtual string Path
        {
            get { return path; }
            protected set { path = value; }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class GetAttribute : HttpMethodAttribute
    {
        public GetAttribute(string path) : base(path) { }

        public override HttpMethod Method
        {
            get { return HttpMethod.Get; }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PostAttribute : HttpMethodAttribute
    {
        public PostAttribute(string path) : base(path) { }

        public override HttpMethod Method
        {
            get { return HttpMethod.Post; }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PutAttribute : HttpMethodAttribute
    {
        public PutAttribute(string path) : base(path) { }

        public override HttpMethod Method
        {
            get { return HttpMethod.Put; }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DeleteAttribute : HttpMethodAttribute
    {
        public DeleteAttribute(string path) : base(path) { }

        public override HttpMethod Method
        {
            get { return HttpMethod.Delete; }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PatchAttribute : HttpMethodAttribute
    {
        public PatchAttribute(string path) : base(path) { }

        public override HttpMethod Method
        {
            get { return new HttpMethod("PATCH"); }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class HeadAttribute : HttpMethodAttribute
    {
        public HeadAttribute(string path) : base(path) { }

        public override HttpMethod Method
        {
            get { return HttpMethod.Head; }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class MultipartAttribute : Attribute { }

    public enum BodySerializationMethod
    {
        /// <summary>
        /// JSON encodes data except for strings. Strings are set as-is
        /// </summary>
        Default,

        /// <summary>
        /// Json encodes everything, including strings
        /// </summary>
        Json,

        /// <summary>
        /// Form-UrlEncode's the values
        /// </summary>
        UrlEncoded
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class BodyAttribute : Attribute
    {
        public BodyAttribute(BodySerializationMethod serializationMethod = BodySerializationMethod.Default,
                             bool buffered = false)
        {
            SerializationMethod = serializationMethod;
            Buffered = buffered;
        }

        public bool Buffered { get; protected set; }
        public BodySerializationMethod SerializationMethod { get; protected set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class AliasAsAttribute : Attribute
    {
        public AliasAsAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; protected set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    [Obsolete("Use Refit.StreamPart, Refit.ByteArrayPart, Refit.FileInfoPart or if necessary, inherit from Refit.MultipartItem", false)]
    public class AttachmentNameAttribute : Attribute
    {
        public AttachmentNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; protected set; }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public class HeadersAttribute : Attribute
    {
        public HeadersAttribute(params string[] headers)
        {
            Headers = headers ?? new string[0];
        }

        public string[] Headers { get; }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header)
        {
            Header = header;
        }

        public string Header { get; }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class AuthorizeAttribute : HeaderAttribute
    {
        public AuthorizeAttribute(string scheme = "Bearer")
            : base("Authorization: " + scheme) { }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)] // Property is to allow for form url encoded data

    public class QueryAttribute : Attribute
    {
        public QueryAttribute() { }

        public QueryAttribute(string delimiter)
        {
            Delimiter = delimiter;
        }

        public QueryAttribute(string delimiter, string prefix)
        {
            Delimiter = delimiter;
            Prefix = prefix;
        }

        public QueryAttribute(string delimiter, string prefix, string format)
        {
            Delimiter = delimiter;
            Prefix = prefix;
            Format = format;
        }

        public string Delimiter { get; protected set; } = ".";
        public string Prefix { get; protected set; }

        public string Format { get; set; }
    }
}
