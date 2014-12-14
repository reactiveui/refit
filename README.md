## Refit: The automatic type-safe REST library for Xamarin and .NET

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

Refit currently supports the following platforms:

* Xamarin.Android
* Xamarin.Mac
* Xamarin.iOS 64-bit (Unified API)
* Desktop .NET 4.5 
* Windows Phone 8
* Windows Store (WinRT) 8.0/8.1
* Windows Phone 8.1 Universal Apps

The following platforms are not supported:

* Xamarin.iOS 32-bit - build system doesn't support targets files

### API Attributes

Every method must have an HTTP attribute that provides the request method and
relative URL. There are five built-in annotations: Get, Post, Put, Delete, and
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
* If the type is `string`, the string will be used directly as the content
* If the parameter has the attribute `[Body(BodySerializationMethod.UrlEncoded)]`, 
  the content will be URL-encoded (see [form posts](#form-posts) below)
* For all other types, the object will be serialized as JSON.

#### JSON content

JSON requests and responses are serialized/deserialized using Json.NET. 
By default, Refit will use the serializer settings that can be configured 
by setting _Newtonsoft.Json.JsonConvert.DefaultSettings_:

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
        JsonSerializerSettings = new JsonSerializerSettings {
            ContractResolver = new SnakeCasePropertyNamesContractResolver()
        }
    });

var otherApi = RestService.For<IOtherApi>("https://api.example.com",
    new RefitSettings {
        JsonSerializerSettings = new JsonSerializerSettings {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        }
    });
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
    public int v { get { return 1; }
 
    [AliasAs("tid")]
    public string WebPropertyId { get; set; }

    [AliasAs("cid")]
    public Guid ClientId { get;set; }

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
with a dynamic  value to a request by applying a `Header` attribute to a parameter:

```csharp
[Get("/users/{user}")]
Task<User> GetUser(string user, [Header("Authorization")] string authorization);

// Will add the header "Authorization: token OAUTH-TOKEN" to the request
var user = await GetUser("octocat", "token OAUTH-TOKEN"); 
```

#### Authorization (Dynamic Headers redux)
The most common reason to use headers is for authorization. Today most API's use some flavor of oAuth with access tokens that expire and refresh tokens that are longer lived.

One way to encapsulate these kinds of token usage, a custom `HttpClientHandler` can be inserted instead. 

For example:
```csharp
class AuthenticatedHttpClientHandler : HttpClientHandler
{
    private readonly Func<Task<string>> getToken;

    public AuthenticatedHttpClientHandler(Func<Task<string>> getToken)
    {
        if (getToken == null) throw new ArgumentNullException("getToken");
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

This class is used like so (example uses the [ADAL](http://msdn.microsoft.com/en-us/library/azure/jj573266.aspx) library to manage auto-token refresh but the principal holds for Xamarin.Auth or any other library:

```csharp
class LoginViewModel
{
	AuthenticationContext context = new AuthenticationContext(...);
	private async Task<string> GetToken()
    {
		// The AquireTokenAsync call will prompt with a UI if necessary
		// Or otherwise silently use a refresh token to return
		// a valid access token	
        var token = await context.AcquireTokenAsync("http://my.service.uri/app", "clientId", new Uri("callback://complete"));

        return token;
    }

	public async void LoginAndCallApi()
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
    [Header("Authorization: Bearer")]
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

### Using generic interfaces

When using something like ASP.NET Web API, it's a fairly common pattern to have a whole stack of CRUD REST services. Refit now supports these, allowing you to define a single API interface with a generic type:

```csharp
public interface IReallyExcitingCrudApi<T, in TKey> where T : class
{
    [Post("")]
    Task<T> Create([Body] T paylod);

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

### What's missing / planned?

Currently Refit is missing the following features from Retrofit that are
planned for a future release soon:

* Multipart requests (including file upload)
