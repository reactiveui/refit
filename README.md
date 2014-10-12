## Refit: The automatic type-safe REST library for Xamarin and .NET

Refit is a library heavily inspired by Square's
[Retrofit](http://square.github.io/retrofit) library, and it turns your REST
API into a live interface:

```cs
public interface IGitHubApi
{
    [Get("/users/{user}")]
    Task<User> GetUser(string user);
}
```

The `RestService` class generates an implementation of `IGitHubApi` that uses
`HttpClient` to make its calls:

```cs
var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com");

var octocat = await gitHubApi.GetUser("octocat");
```

### Where does this work?

Refit currently supports the following platforms:

* Xamarin.Android
* Xamarin.Mac
* Xamarin.iOS 64-bit
* Desktop .NET 4.5 
* Windows Phone 8

Support for the following platforms will probably not happen because they're
too broken:

* Silverlight 5
* Windows Store (WinRT)

### API Attributes

Every method must have an HTTP attribute that provides the request method and
relative URL. There are five built-in annotations: Get, Post, Put, Delete, and
Head. The relative URL of the resource is specified in the annotation.

```cs
[Get("/users/list")]
```

You can also specify query parameters in the URL:

```cs
[Get("/users/list?sort=desc")]
```

A request URL can be updated dynamically using replacement blocks and
parameters on the method. A replacement block is an alphanumeric string
surrounded by { and }. 

If the name of your parameter doesn't match the name in the URL path, use the
`AliasAs` attribute.

```cs
[Get("/group/{id}/users")]
Task<List<User>> GroupList([AliasAs("id")] int groupId);
```

Parameters that are not specified as a URL substitution will automatically be
used as query parameters. This is different than Retrofit, where all
parameters must be explicitly specified.

One thing that is important to note, is that **URL paths must be lowercase**.
URLs are case-insensitive in the HTTP spec, so we reflect that by being picky
about case :trollface:

However, the comparison between parameter name and URL parameter is *not*
case-sensitive, so it will work correctly if you name your parameter `groupId`
in the path `/group/{groupid}/show` for example.

```cs
[Get("/group/{id}/users")]
Task<List<User>> GroupList([AliasAs("id")] int groupId, [AliasAs("sort")] string sortOrder);

GroupList(4, "desc");
>>> "/group/4/users?sort=desc"
```

### Body content

One of the parameters in your method can be used as the body, by using the
Body attribute:

```cs
[Post("/users/new")]
Task CreateUser([Body] User user);
```

There are three possibilities for supplying the body data, depending on the
type of the parameter:

* If the type is `Stream`, the content will be streamed via `StreamContent`
* If the type is `string`, the string will be used directly as the content
* For all other types, the object will be serialized as JSON.

#### Sending Form Posts

The Body attribute also supports encoding its data as a Form POST - to do
this, initialize the `[Body]` attribute with `BodySerializationMethod.UrlEncoded`:

```cs
[Post("/api/auth.signin")]
Task<AuthenticationResult> Login([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> form);
```

Simple objects can also be used as forms, where the key will be the property
name - the `AliasAs` attribute works here as well:

```cs
public interface IMeasurementProtocolApi
{
    [Post("/collect")]
    Task Collect([Body(BodySerializationMethod.UrlEncoded)] Measurement measurement);
}

public Measurement
{
    [AliasAs("t")] 
    public string Type { get; set; }
}
```

### Setting request headers

You can set one or more static request headers for a request applying a `Headers` 
attribute to the method:

```cs
[Headers("User-Agent: Awesome Octocat App")]
[Get("/users/{user}")]
Task<User> GetUser(string user);
```

Static headers can also be added to _every request in the API_ by applying the 
`Headers` attribute to the interface:

```cs
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

```cs
[Get("/users/{user}")]
Task<User> GetUser(string user, [Header("Authorization")] string authorization);

// Will add the header "Authorization: token OAUTH-TOKEN" to the request
var user = await GetUser("octocat", "token OAUTH-TOKEN"); 
```

#### Redefining headers

Unlike Retrofit, where headers do not overwrite each other and are all added to 
the request regardless of how many times the same header is defined, Refit takes 
a similar approach to the approach ASP.NET MVC takes with action filters &mdash; 
**redefining a header will replace it**, in the following order of precedence:

* `Headers` attribute on the interface _(lowest priority)_
* `Headers` attribute on the method
* `Header` attribute on a method parameter _(highest priority)_

```cs
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

```cs
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

```cs
[Post("/users/new")]
Task CreateUser([Body] User user);

// This will throw if the network call fails
await CreateUser(someUser);
```

If the type parameter is 'HttpResponseMessage' or 'string', the raw response
message or the content as a string will be returned respectively.

```cs
// Returns the content as a string (i.e. the JSON data)
[Get("/users/{user}")]
Task<string> GetUser(string user);

// Returns the raw response, as an IObservable that can be used with the
// Reactive Extensions
[Get("/users/{user}")]
IObservable<HttpResponseMessage> GetUser(string user);
```

### What's missing / planned?

Currently Refit is missing the following features from Retrofit that are
planned for a future release soon:

* Multipart requests (including file upload)
