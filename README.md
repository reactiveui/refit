![Refit](refit_logo.png)

## Refit: The automatic type-safe REST library for .NET Core, Xamarin and .NET

[![Build Status](https://dev.azure.com/dotnet/ReactiveUI/_apis/build/status/Refit-CI?branchName=master)](https://dev.azure.com/dotnet/ReactiveUI/_build/latest?definitionId=17)

||Refit|Refit.HttpClientFactory|
|-|-|-|
|*NuGet*|[![NuGet](https://img.shields.io/nuget/v/Refit.svg)](https://www.nuget.org/packages/Refit/)|[![NuGet](https://img.shields.io/nuget/v/Refit.HttpClientFactory.svg)](https://www.nuget.org/packages/Refit.HttpClientFactory/)|
|*Azure<br />Artifacts*|[![Refit package in Refit feed in Azure Artifacts](https://feeds.dev.azure.com/dotnet/a5852744-a77d-4d76-a9d2-81ac1fdd5744/_apis/public/Packaging/Feeds/6368ad76-9653-4c2f-a6c6-da8c790ae826/Packages/2f65cb3b-df09-4050-a84e-b868ed95fd28/Badge)](https://dev.azure.com/dotnet/ReactiveUI/_packaging?_a=package&feed=6368ad76-9653-4c2f-a6c6-da8c790ae826&package=2f65cb3b-df09-4050-a84e-b868ed95fd28&preferRelease=true)|[![Refit.HttpClientFactory package in Refit feed in Azure Artifacts](https://feeds.dev.azure.com/dotnet/a5852744-a77d-4d76-a9d2-81ac1fdd5744/_apis/public/Packaging/Feeds/6368ad76-9653-4c2f-a6c6-da8c790ae826/Packages/d87934cb-2f7b-44b7-83b8-3872ac965ef2/Badge)](https://dev.azure.com/dotnet/ReactiveUI/_packaging?_a=package&feed=6368ad76-9653-4c2f-a6c6-da8c790ae826&package=d87934cb-2f7b-44b7-83b8-3872ac965ef2&preferRelease=true)|

CI Feed: `https://pkgs.dev.azure.com/dotnet/ReactiveUI/_packaging/Refit/nuget/v3/index.json`

Refit is a library heavily inspired by Square's
[Retrofit](http://square.github.io/retrofit) library, and it turns your REST
API into a live interface:

```csharp
public interface IGitHubApi
{
    [Get("/users/{user}")]
    Task<User> GetUser(string user);
}
```

The `RestService` class generates an implementation of `IGitHubApi` that uses
`HttpClient` to make its calls:

```csharp
var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com");

var octocat = await gitHubApi.GetUser("octocat");
```

### Where does this work?

Refit currently supports the following platforms and any .NET Standard 2.0 target:

* UWP
* Xamarin.Android
* Xamarin.Mac
* Xamarin.iOS 
* Desktop .NET 4.6.1 
* .NET Core
* Uno Platform

#### Note about .NET Core
For .NET Core build-time support, you must use the .NET Core 2 SDK. You can target any supported platform in your library, long as the 2.0+ SDK is used at build-time.

### API Attributes

Every method must have an HTTP attribute that provides the request method and
relative URL. There are six built-in annotations: Get, Post, Put, Delete, Patch and
Head. The relative URL of the resource is specified in the annotation.

```csharp
[Get("/users/list")]
```

You can also specify query parameters in the URL:

```csharp
[Get("/users/list?sort=desc")]
```

A request URL can be updated dynamically using replacement blocks and
parameters on the method. A replacement block is an alphanumeric string
surrounded by { and }. 

If the name of your parameter doesn't match the name in the URL path, use the
`AliasAs` attribute.

```csharp
[Get("/group/{id}/users")]
Task<List<User>> GroupList([AliasAs("id")] int groupId);
```

A request url can also bind replacement blocks to a custom object

```csharp
[Get("/group/{request.groupId}/users/{request.userId}")]
Task<List<User>> GroupList(UserGroupRequest request);

class UserGroupRequest{
    int groupId { get;set; }
    int userId { get;set; }
}

```

Parameters that are not specified as a URL substitution will automatically be
used as query parameters. This is different than Retrofit, where all
parameters must be explicitly specified.

The comparison between parameter name and URL parameter is *not*
case-sensitive, so it will work correctly if you name your parameter `groupId`
in the path `/group/{groupid}/show` for example.

```csharp
[Get("/group/{id}/users")]
Task<List<User>> GroupList([AliasAs("id")] int groupId, [AliasAs("sort")] string sortOrder);

GroupList(4, "desc");
>>> "/group/4/users?sort=desc"
```

Round-tripping route parameter syntax: Forward slashes aren't encoded when using a double-asterisk (\*\*) catch-all parameter syntax.

During link generation, the routing system encodes the value captured in a double-asterisk (\*\*) catch-all parameter (for example, {**myparametername}) except the forward slashes.

The type of round-tripping route parameter must be string.

```csharp
[Get("/search/{**page}")]
Task<List<Page>> Search(string page);

Search("admin/products");
>>> "/search/admin/products"
```

### Dynamic Querystring Parameters

If you specify an `object` as a query parameter, all public properties which are not null are used as query parameters.
This previously only applied to GET requests, but has now been expanded to all HTTP request methods, partly thanks to Twitter's hybrid API that insists on non-GET requests with querystring parameters.
Use the `Query` attribute the change the behavior to 'flatten' your query parameter object. If using this Attribute you can specify values for the Delimiter and the Prefix which are used to 'flatten' the object.

```csharp
public class MyQueryParams
{
    [AliasAs("order")]
    public string SortOrder { get; set; }

    public int Limit { get; set; }
}


[Get("/group/{id}/users")]
Task<List<User>> GroupList([AliasAs("id")] int groupId, MyQueryParams params);

[Get("/group/{id}/users")]
Task<List<User>> GroupListWithAttribute([AliasAs("id")] int groupId, [Query(".","search")] MyQueryParams params);


params.SortOrder = "desc";
params.Limit = 10;

GroupList(4, params)
>>> "/group/4/users?order=desc&Limit=10"

GroupListWithAttribute(4, params)
>>> "/group/4/users?search.order=desc&search.Limit=10"
```

A similar behavior exists if using a Dictionary, but without the advantages of the `AliasAs` attributes and of course no intellisense and/or type safety.

You can also specify querystring parameters with [Query] and have them flattened in non-GET requests, similar to:
```csharp
[Post("/statuses/update.json")]
Task<Tweet> PostTweet([Query]TweetParams params);
```

Where `TweetParams` is a POCO, and properties will also support `[AliasAs]` attributes.

### Collections as Querystring parameters

Use the `Query` attribute to specify format in which collections should be formatted in query string

```csharp
[Get("/users/list")]
Task Search([Query(CollectionFormat.Multi)]int[] ages);

Search(new [] {10, 20, 30})
>>> "/users/list?ages=10&ages=20&ages=30"

[Get("/users/list")]
Task Search([Query(CollectionFormat.Csv)]int[] ages);

Search(new [] {10, 20, 30})
>>> "/users/list?ages=10%2C20%2C30"
```

You can also specify collection format in `RefitSettings`, that will be used by default, unless explicitly defined in `Query` attribute.

```csharp
var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com",
    new RefitSettings {
        CollectionFormat = CollectionFormat.Multi
    });
```

### Unescape Querystring parameters

Use the `QueryUriFormat` attribute to specify if the query parameters should be url escaped

```csharp
[Get("/query")]
[QueryUriFormat(UriFormat.Unescaped)]
Task Query(string q);

Query("Select+Id,Name+From+Account")
>>> "/query?q=Select+Id,Name+From+Account"
```

### Body content

One of the parameters in your method can be used as the body, by using the
Body attribute:

```csharp
[Post("/users/new")]
Task CreateUser([Body] User user);
```

There are four possibilities for supplying the body data, depending on the
type of the parameter:

* If the type is `Stream`, the content will be streamed via `StreamContent`
* If the type is `string`, the string will be used directly as the content unless `[Body(BodySerializationMethod.Json)]` is set which will send it as a `StringContent`
* If the parameter has the attribute `[Body(BodySerializationMethod.UrlEncoded)]`, 
  the content will be URL-encoded (see [form posts](#form-posts) below)
* For all other types, the object will be serialized using the content serializer specified in 
RefitSettings (JSON is the default).

#### Buffering and the `Content-Length` header

By default, Refit streams the body content without buffering it. This means you can
stream a file from disk, for example, without incurring the overhead of loading 
the whole file into memory. The downside of this is that no `Content-Length` header 
is set _on the request_. If your API needs you to send a `Content-Length` header with
the request, you can disable this streaming behavior by setting the `buffered` argument 
of the `[Body]` attribute to `true`:

```csharp
Task CreateUser([Body(buffered: true)] User user);
```

#### JSON content

JSON requests and responses are serialized/deserialized using an instance of the `IContentSerializer` interface. Refit provides two implementations out of the box: `NewtonsoftJsonContentSerializer` (which is the default JSON serializer) and `SystemTextJsonContentSerializer`. The first uses the well known `Newtonsoft.Json` library and is extremely versatile and customizable, while the latter uses the new `System.Text.Json` APIs and is focused on high performance and low memory usage, at the cost of being slightly less feature rich. You can read more about the two serializers and the main differences between the two [at this link](https://docs.microsoft.com/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to).

For instance, here is how to create a new `RefitSettings` instance using the `System.Text.Json`-based serializer:

```csharp
var settings = new RefitSettings(new SystemTextJsonContentSerializer());
```

If instead you're using the default settings, which use the `Newtonsoft.Json` APIs, you can customize their behavior by setting the `Newtonsoft.Json.JsonConvert.DefaultSettings` property:

```csharp
JsonConvert.DefaultSettings = 
    () => new JsonSerializerSettings() { 
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters = {new StringEnumConverter()}
    };

// Serialized as: {"day":"Saturday"}
await PostSomeStuff(new { Day = DayOfWeek.Saturday });
```

As these are global settings they will affect your entire application. It
might be beneficial to isolate the settings for calls to a particular API. 
When creating a Refit generated live interface, you may optionally pass a 
`RefitSettings` that will allow you to specify what serializer settings you 
would like. This allows you to have different serializer settings for separate
APIs:

```csharp
var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com",
    new RefitSettings {
        ContentSerializer = new NewtonsoftJsonContentSerializer( 
            new JsonSerializerSettings {
                ContractResolver = new SnakeCasePropertyNamesContractResolver()
        }
    )});

var otherApi = RestService.For<IOtherApi>("https://api.example.com",
    new RefitSettings {
        ContentSerializer = new NewtonsoftJsonContentSerializer( 
            new JsonSerializerSettings {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
        }
    )});
```

Property serialization/deserialization can be customised using Json.NET's 
JsonProperty attribute:

```csharp 
public class Foo 
{
    // Works like [AliasAs("b")] would in form posts (see below)
    [JsonProperty(PropertyName="b")] 
    public string Bar { get; set; }
} 
```

#### XML Content

XML requests and responses are serialized/deserialized using _System.Xml.Serialization.XmlSerializer_. 
By default, Refit will use JSON content serialization, to use XML content configure the ContentSerializer to use the `XmlContentSerializer`:

```csharp
var gitHubApi = RestService.For<IXmlApi>("https://www.w3.org/XML",
    new RefitSettings {
        ContentSerializer = new XmlContentSerializer()
    });
```

Property serialization/deserialization can be customised using   attributes found in the _System.Xml.Serialization_ namespace:

```csharp
    public class Foo
    {
        [XmlElement(Namespace = "https://www.w3.org/XML")]
        public string Bar { get; set; }
    }
```

The _System.Xml.Serialization.XmlSerializer_ provides many options for serializing, those options can be set by providing an `XmlContentSerializerSettings` to the `XmlContentSerializer` constructor:

```csharp
var gitHubApi = RestService.For<IXmlApi>("https://www.w3.org/XML",
    new RefitSettings {
        ContentSerializer = new XmlContentSerializer(
            new XmlContentSerializerSettings
            {
                XmlReaderWriterSettings = new XmlReaderWriterSettings()
                {
                    ReaderSettings = new XmlReaderSettings
                    {
                        IgnoreWhitespace = true
                    }
                }
            }
        )
    });
```

#### <a name="form-posts"></a>Form posts

For APIs that take form posts (i.e. serialized as `application/x-www-form-urlencoded`),
initialize the Body attribute with `BodySerializationMethod.UrlEncoded`.

The parameter can be an `IDictionary`:

```csharp
public interface IMeasurementProtocolApi
{
    [Post("/collect")]
    Task Collect([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);
}

var data = new Dictionary<string, object> {
    {"v", 1}, 
    {"tid", "UA-1234-5"}, 
    {"cid", new Guid("d1e9ea6b-2e8b-4699-93e0-0bcbd26c206c")}, 
    {"t", "event"},
};

// Serialized as: v=1&tid=UA-1234-5&cid=d1e9ea6b-2e8b-4699-93e0-0bcbd26c206c&t=event
await api.Collect(data);
```

Or you can just pass any object and all _public, readable_ properties will 
be serialized as form fields in the request. This approach allows you to alias 
property names using `[AliasAs("whatever")]` which can help if the API has
cryptic field names:

```csharp
public interface IMeasurementProtocolApi
{
    [Post("/collect")]
    Task Collect([Body(BodySerializationMethod.UrlEncoded)] Measurement measurement);
}

public class Measurement
{
    // Properties can be read-only and [AliasAs] isn't required
    public int v { get { return 1; } }
 
    [AliasAs("tid")]
    public string WebPropertyId { get; set; }

    [AliasAs("cid")]
    public Guid ClientId { get; set; }

    [AliasAs("t")] 
    public string Type { get; set; }

    public object IgnoreMe { private get; set; }
}

var measurement = new Measurement { 
    WebPropertyId = "UA-1234-5", 
    ClientId = new Guid("d1e9ea6b-2e8b-4699-93e0-0bcbd26c206c"), 
    Type = "event" 
}; 

// Serialized as: v=1&tid=UA-1234-5&cid=d1e9ea6b-2e8b-4699-93e0-0bcbd26c206c&t=event
await api.Collect(measurement);
``` 

If you have a type that has `[JsonProperty(PropertyName)]` attributes setting property aliases, Refit will use those too (`[AliasAs]` will take precedence where you have both). 
This means that the following type will serialize as `one=value1&two=value2`:

```csharp

public class SomeObject
{
    [JsonProperty(PropertyName = "one")]
    public string FirstProperty { get; set; }

    [JsonProperty(PropertyName = "notTwo")]
    [AliasAs("two")]
    public string SecondProperty { get; set; }
}

```

**NOTE:** This use of `AliasAs` applies to querystring parameters and form body posts, but not to response objects; for aliasing fields on response objects, you'll still need to use `[JsonProperty("full-property-name")]`.

### Setting request headers

#### Static headers

You can set one or more static request headers for a request applying a `Headers` 
attribute to the method:

```csharp
[Headers("User-Agent: Awesome Octocat App")]
[Get("/users/{user}")]
Task<User> GetUser(string user);
```

Static headers can also be added to _every request in the API_ by applying the 
`Headers` attribute to the interface:

```csharp
[Headers("User-Agent: Awesome Octocat App")]
public interface IGitHubApi
{
    [Get("/users/{user}")]
    Task<User> GetUser(string user);
    
    [Post("/users/new")]
    Task CreateUser([Body] User user);
}
```

#### Dynamic headers

If the content of the header needs to be set at runtime, you can add a header
with a dynamic value to a request by applying a `Header` attribute to a parameter:

```csharp
[Get("/users/{user}")]
Task<User> GetUser(string user, [Header("Authorization")] string authorization);

// Will add the header "Authorization: token OAUTH-TOKEN" to the request
var user = await GetUser("octocat", "token OAUTH-TOKEN"); 
```

#### Authorization (Dynamic Headers redux)
The most common reason to use headers is for authorization. Today most API's use some flavor of oAuth with access tokens that expire and refresh tokens that are longer lived.

One way to encapsulate these kinds of token usage, a custom `HttpClientHandler` can be inserted instead.  
There are two classes for doing this: one is `AuthenticatedHttpClientHandler`, which takes a `Func<Task<string>>` parameter, where a signature can be generated without knowing about the request.
The other is `AuthenticatedParameterizedHttpClientHandler`, which takes a `Func<HttpRequestMessage, Task<string>>` parameter, where the signature requires information about the request (see earlier notes about Twitter's API)

For example:
```csharp
class AuthenticatedHttpClientHandler : HttpClientHandler
{
    private readonly Func<Task<string>> getToken;

    public AuthenticatedHttpClientHandler(Func<Task<string>> getToken)
    {
        if (getToken == null) throw new ArgumentNullException(nameof(getToken));
        this.getToken = getToken;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // See if the request has an authorize header
        var auth = request.Headers.Authorization;
        if (auth != null)
        {
            var token = await getToken().ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue(auth.Scheme, token);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
```

Or:

```csharp
class AuthenticatedParameterizedHttpClientHandler : DelegatingHandler
    {
        readonly Func<HttpRequestMessage, Task<string>> getToken;

        public AuthenticatedParameterizedHttpClientHandler(Func<HttpRequestMessage, Task<string>> getToken, HttpMessageHandler innerHandler = null)
            : base(innerHandler ?? new HttpClientHandler())
        {
            this.getToken = getToken ?? throw new ArgumentNullException(nameof(getToken));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // See if the request has an authorize header
            var auth = request.Headers.Authorization;
            if (auth != null)
            {
                var token = await getToken(request).ConfigureAwait(false);
                request.Headers.Authorization = new AuthenticationHeaderValue(auth.Scheme, token);
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
```

While HttpClient contains a nearly identical method signature, it is used differently. HttpClient.SendAsync is not called by Refit. The HttpClientHandler must be modified instead.

This class is used like so (example uses the [ADAL](http://msdn.microsoft.com/en-us/library/azure/jj573266.aspx) library to manage auto-token refresh but the principal holds for Xamarin.Auth or any other library:

```csharp
class LoginViewModel
{
    AuthenticationContext context = new AuthenticationContext(...);
    
    private async Task<string> GetToken()
    {
        // The AcquireTokenAsync call will prompt with a UI if necessary
        // Or otherwise silently use a refresh token to return
        // a valid access token	
        var token = await context.AcquireTokenAsync("http://my.service.uri/app", "clientId", new Uri("callback://complete"));
        
        return token;
    }

    public async Task LoginAndCallApi()
    {
        var api = RestService.For<IMyRestService>(new HttpClient(new AuthenticatedHttpClientHandler(GetToken)) { BaseAddress = new Uri("https://the.end.point/") });
        var location = await api.GetLocationOfRebelBase();
    }
}

interface IMyRestService
{
    [Get("/getPublicInfo")]
    Task<Foobar> SomePublicMethod();

    [Get("/secretStuff")]
    [Headers("Authorization: Bearer")]
    Task<Location> GetLocationOfRebelBase();
}
```

In the above example, any time a method that requires authentication is called, the `AuthenticatedHttpClientHandler` will try to get a fresh access token. It's up to the app to provide one, checking the expiration time of an existing access token and obtaining a new one if needed. 

#### Redefining headers

Unlike Retrofit, where headers do not overwrite each other and are all added to 
the request regardless of how many times the same header is defined, Refit takes 
a similar approach to the approach ASP.NET MVC takes with action filters &mdash; 
**redefining a header will replace it**, in the following order of precedence:

* `Headers` attribute on the interface _(lowest priority)_
* `Headers` attribute on the method
* `Header` attribute on a method parameter _(highest priority)_

```csharp
[Headers("X-Emoji: :rocket:")]
public interface IGitHubApi
{
    [Get("/users/list")]
    Task<List> GetUsers();
    
    [Get("/users/{user}")]
    [Headers("X-Emoji: :smile_cat:")]
    Task<User> GetUser(string user);
    
    [Post("/users/new")]
    [Headers("X-Emoji: :metal:")]
    Task CreateUser([Body] User user, [Header("X-Emoji")] string emoji);
}

// X-Emoji: :rocket:
var users = await GetUsers();

// X-Emoji: :smile_cat:
var user = await GetUser("octocat");

// X-Emoji: :trollface:
await CreateUser(user, ":trollface:");
```

**Note:** This redefining behavior only applies to headers _with the same name_. Headers with different names are not replaced. The following code will result in all headers being included:

```csharp
[Headers("Header-A: 1")]
public interface ISomeApi
{
    [Headers("Header-B: 2")]
    [Post("/post")]
    Task PostTheThing([Header("Header-C")] int c);
}

// Header-A: 1
// Header-B: 2
// Header-C: 3
var user = await api.PostTheThing(3);
```

#### Removing headers

Headers defined on an interface or method can be removed by redefining 
a static header without a value (i.e. without `: <value>`) or passing `null` for 
a dynamic header. _Empty strings will be included as empty headers._

```csharp
[Headers("X-Emoji: :rocket:")]
public interface IGitHubApi
{
    [Get("/users/list")]
    [Headers("X-Emoji")] // Remove the X-Emoji header
    Task<List> GetUsers();
    
    [Get("/users/{user}")]
    [Headers("X-Emoji:")] // Redefine the X-Emoji header as empty
    Task<User> GetUser(string user);
    
    [Post("/users/new")]
    Task CreateUser([Body] User user, [Header("X-Emoji")] string emoji);
}

// No X-Emoji header
var users = await GetUsers();

// X-Emoji: 
var user = await GetUser("octocat");

// No X-Emoji header
await CreateUser(user, null); 

// X-Emoji: 
await CreateUser(user, ""); 
```

### Multipart uploads

Methods decorated with `Multipart` attribute will be submitted with multipart content type.
At this time, multipart methods support the following parameter types:

 - string (parameter name will be used as name and string value as value)
 - byte array
 - Stream
 - FileInfo

The parameter name will be used as the name of the field in the multipart data. This can be overridden with the `AliasAs` attribute.
A custom boundary can be specified with an optional string parameter to the `Multipart` attribute. If left empty, this defaults to `----MyGreatBoundary`.

To specify the file name and content type for byte array (`byte[]`), `Stream` and `FileInfo` parameters, use of a wrapper class is required.
The wrapper classes for these types are `ByteArrayPart`, `StreamPart` and `FileInfoPart`.

```csharp
public interface ISomeApi
{
    [Multipart]
    [Post("/users/{id}/photo")]
    Task UploadPhoto(int id, [AliasAs("myPhoto")] StreamPart stream);
}
```

To pass a Stream to this method, construct a StreamPart object like so:

```csharp
someApiInstance.UploadPhoto(id, new StreamPart(myPhotoStream, "photo.jpg", "image/jpeg"));
```

Note: The AttachmentName attribute that was previously described in this section has been deprecated and its use is not recommended.

### Retrieving the response

Note that in Refit unlike in Retrofit, there is no option for a synchronous
network request - all requests must be async, either via `Task` or via
`IObservable`. There is also no option to create an async method via a Callback
parameter unlike Retrofit, because we live in the async/await future.

Similarly to how body content changes via the parameter type, the return type
will determine the content returned.

Returning Task without a type parameter will discard the content and solely
tell you whether or not the call succeeded:

```csharp
[Post("/users/new")]
Task CreateUser([Body] User user);

// This will throw if the network call fails
await CreateUser(someUser);
```

If the type parameter is 'HttpResponseMessage' or 'string', the raw response
message or the content as a string will be returned respectively.

```csharp
// Returns the content as a string (i.e. the JSON data)
[Get("/users/{user}")]
Task<string> GetUser(string user);

// Returns the raw response, as an IObservable that can be used with the
// Reactive Extensions
[Get("/users/{user}")]
IObservable<HttpResponseMessage> GetUser(string user);
```

There is also a generic wrapper class called `ApiResponse<T>` that can be used as a return type. Using this class as a return type allows you to retrieve not just the content as an object, but also any meta data associated with the request/response. This includes information such as response headers, the http status code and reason phrase (e.g. 404 Not Found), the response version, the original request message that was sent and in the case of an error, an `ApiException` object containing details of the error. Following are some examples of how you can retrieve the response meta data.

```csharp
//Returns the content within a wrapper class containing meta data about the request/response
[Get("/users/{user}")]
Task<ApiResponse<User>> GetUser(string user);

//Calling the API
var response = await gitHubApi.GetUser("octocat");

//Getting the status code (returns a value from the System.Net.HttpStatusCode enumeration)
var httpStatus = response.StatusCode;

//Determining if a success status code was received
if(response.IsSuccessStatusCode)
{
    //YAY! Do the thing...
}

//Retrieving a well-known header value (e.g. "Server" header)
var serverHeaderValue = response.Headers.Server != null ? response.Headers.Server.ToString() : string.Empty;

//Retrieving a custom header value
var customHeaderValue = string.Join(',', response.Headers.GetValues("A-Custom-Header"));

//Looping through all the headers
foreach(var header in response.Headers)
{
    var headerName = header.Key;
    var headerValue = string.Join(',', header.Value);
}

//Finally, retrieving the content in the response body as a strongly-typed object
var user = response.Content;
```

### Using generic interfaces

When using something like ASP.NET Web API, it's a fairly common pattern to have a whole stack of CRUD REST services. Refit now supports these, allowing you to define a single API interface with a generic type:

```csharp
public interface IReallyExcitingCrudApi<T, in TKey> where T : class
{
    [Post("")]
    Task<T> Create([Body] T payload);

    [Get("")]
    Task<List<T>> ReadAll();

    [Get("/{key}")]
    Task<T> ReadOne(TKey key);

    [Put("/{key}")]
    Task Update(TKey key, [Body]T payload);

    [Delete("/{key}")]
    Task Delete(TKey key);
}
```

Which can be used like this:

```csharp
// The "/users" part here is kind of important if you want it to work for more 
// than one type (unless you have a different domain for each type)
var api = RestService.For<IReallyExcitingCrudApi<User, string>>("http://api.example.com/users"); 
``` 
### Interface inheritance

When multiple services that need to be kept separate share a number of APIs, it is possible to leverage interface inheritance to avoid having to define the same Refit methods multiple times in different services:

```csharp
public interface IBaseService
{
    [Get("/resources")]
    Task<Resource> GetResource(string id);
}

public interface IDerivedServiceA : IBaseService
{
    [Delete("/resources")]
    Task DeleteResource(string id);
}

public interface IDerivedServiceB : IBaseService
{
    [Post("/resources")]
    Task<string> AddResource([Body] Resource resource);
}
```

In this example, the `IDerivedServiceA` interface will expose both the `GetResource` and `DeleteResource` APIs, while `IDerivedServiceB` will expose `GetResource` and `AddResource`.

#### Headers inheritance

When using inheritance, existing header attributes will passed along as well, and the inner-most ones will have precedence:

```csharp
[Headers("User-Agent: AAA")]
public interface IAmInterfaceA
{
    [Get("/get?result=Ping")]
    Task<string> Ping();
}

[Headers("User-Agent: BBB")]
public interface IAmInterfaceB : IAmInterfaceA
{
    [Get("/get?result=Pang")]
    [Headers("User-Agent: PANG")]
    Task<string> Pang();

    [Get("/get?result=Foo")]
    Task<string> Foo();
}
```

Here, `IAmInterfaceB.Pang()` will use `PANG` as its user agent, while `IAmInterfaceB.Foo` and `IAmInterfaceB.Ping` will use `BBB`.
Note that if `IAmInterfaceB` didn't have a header attribute, `Foo` would then use the `AAA` value inherited from `IAmInterfaceA`.
If an interface is inheriting more than one interface, the order of precedence is the same as the one in which the inherited interfaces are declared:

```csharp
public interface IAmInterfaceC : IAmInterfaceA, IAmInterfaceB
{
    [Get("/get?result=Foo")]
    Task<string> Foo();
}
```

Here `IAmInterfaceC.Foo` would use the header attribute inherited from `IAmInterfaceA`, if present, or the one inherited from `IAmInterfaceB`, and so on for all the declared interfaces.

### Using HttpClientFactory

Refit has first class support for the ASP.Net Core 2.1 HttpClientFactory. Add a reference to `Refit.HttpClientFactory` and call 
the provided extension method in your `ConfigureServices` method to configure your Refit interface:

```csharp
services.AddRefitClient<IWebApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"));
        // Add additional IHttpClientBuilder chained methods as required here:
        // .AddHttpMessageHandler<MyHandler>()
        // .SetHandlerLifetime(TimeSpan.FromMinutes(2));
```

Optionally, a `RefitSettings` object can be included: 
```csharp
var settings = new RefitSettings(); 
// Configure refit settings here

services.AddRefitClient<IWebApi>(settings)
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"));
        // Add additional IHttpClientBuilder chained methods as required here:
        // .AddHttpMessageHandler<MyHandler>()
        // .SetHandlerLifetime(TimeSpan.FromMinutes(2));
```
Note that some of the properties of `RefitSettings` will be ignored because the `HttpClient` and `HttpClientHandlers` will be managed by the `HttpClientFactory` instead of Refit.

You can then get the api interface using constructor injection:

```csharp
public class HomeController : Controller
{
    public HomeController(IWebApi webApi)
    {
        _webApi = webApi;
    }

    private readonly IWebApi _webApi;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var thing = await _webApi.GetSomethingWeNeed(cancellationToken);
        return View(thing);
    }
}
```

### Handling exceptions

To encapsulate any exceptions that may come from a service, you can catch an `ApiException` which contains request- and response information. Refit also supports the catching of validation exceptions that are thrown by a service implementing the RFC 7807 specification for problem details due to bad requests. For specific information on the problem details of the validation exception, simply catch `ValidationApiException`:

```csharp
// ...
try
{
   var result = await awesomeApi.GetFooAsync("bar");
}
catch (ValidationApiException validationException)
{
   // handle validation here by using validationException.Content,
   // which is type of ProblemDetails according to RFC 7807

   // If the response contains additional properties on the problem details,
   // they will be added to the validationException.Content.Extensions collection.
}
catch (ApiException exception)
{
   // other exception handling
}
// ...
```
