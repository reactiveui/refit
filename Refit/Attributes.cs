using System;
using System.Net.Http;

namespace Refit
{
    public abstract class HttpMethodAttribute : Attribute
    {
        public abstract HttpMethod Method { get; }

        protected string path;
        public virtual string Path {
            get { return path; }
            protected set { path = value; }
        }

        public HttpMethodAttribute(string path)
        {
            Path = path;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class GetAttribute : HttpMethodAttribute
    {
        public GetAttribute(string path) : base(path) {}

        public override HttpMethod Method {
            get { return HttpMethod.Get; }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PostAttribute : HttpMethodAttribute
    {
        public PostAttribute(string path) : base(path) {}

        public override HttpMethod Method {
            get { return HttpMethod.Post; }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PutAttribute : HttpMethodAttribute
    {
        public PutAttribute(string path) : base(path) {}

        public override HttpMethod Method {
            get { return HttpMethod.Put; }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DeleteAttribute : HttpMethodAttribute
    {
        public DeleteAttribute(string path) : base(path) {}

        public override HttpMethod Method {
            get { return HttpMethod.Delete; }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PatchAttribute : HttpMethodAttribute
    {
        public PatchAttribute(string path) : base(path) { }

        public override HttpMethod Method {
            get { return new HttpMethod("PATCH"); }
        }
    }
        
    [AttributeUsage(AttributeTargets.Method)]
    public class HeadAttribute : HttpMethodAttribute
    {
        public HeadAttribute(string path) : base(path) {}

        public override HttpMethod Method {
            get { return HttpMethod.Head; }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class MultipartAttribute : Attribute {
    }

    public enum BodySerializationMethod {
        Json, UrlEncoded
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class BodyAttribute : Attribute
    {
        public BodySerializationMethod SerializationMethod { get; protected set; }

        public bool Buffered { get; protected set; }

        public BodyAttribute(BodySerializationMethod serializationMethod = BodySerializationMethod.Json,
            bool buffered = false)
        {
            SerializationMethod = serializationMethod;
            Buffered = buffered;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class AliasAsAttribute : Attribute
    {
        public string Name { get; protected set; }
        public AliasAsAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)][Obsolete("Use Refit.StreamPart, Refit.ByteArrayPart, Refit.FileInfoPart or if necessary, inherit from Refit.MultipartItem", false)]
    public class AttachmentNameAttribute : Attribute
    {
        public string Name { get; protected set; }
        public AttachmentNameAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public class HeadersAttribute : Attribute
    {
        public string[] Headers { get; private set; }

        public HeadersAttribute(params string[] headers)
        {
            Headers = headers ?? new string[0];
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class HeaderAttribute : Attribute
    {
        public string Header { get; private set; }

        public HeaderAttribute(string header)
        {
            Header = header;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class AuthorizeAttribute : HeaderAttribute
    {
        public AuthorizeAttribute(string scheme = "Bearer")
            : base("Authorization: " + scheme)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class QueryAttribute : Attribute
    {
        public string Delimiter { get; protected set; } = ".";
        public string Prefix { get; protected set; }

        public QueryAttribute()
        {
        }

        public QueryAttribute(string delimiter)
        {
            Delimiter = delimiter;
        }

        public QueryAttribute(string delimiter, string prefix)
        {
            Delimiter = delimiter;
            Prefix = prefix;
        }

    }
}
