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
* Desktop .NET 4.5 
* Windows Phone 8
* Silverlight 5

Support for the following platforms is coming soon:

* Xamarin.iOS
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

```cs
[Get("/group/{id}/users")]
Task<List<User>> GroupList([AliasAs("id")] int groupId, [AliasAs("sort")] string sortOrder);

GroupList(4, "desc");
>>> "/group/4/users?sort=desc"
```

### Body content

One of the parameters in your method can be used as the body, by using the
Body attribute:

```
[Post("/users/new")]
Task CreateUser([Body] User user);
```

There are three possibilities for supplying the body data, depending on the
type of the parameter:

* If the type is `Stream`, the content will be streamed via `StreamContent`
* If the type is `string`, the string will be used directly as the content
* For all other types, the object will be serialized as JSON.

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
IObservable<string> GetUser(string user);
```

### What's missing / planned?

Currently Refit is missing the following features from Retrofit that are
planned for a future release soon:

* Multipart requests (including file upload)
* Form posts
