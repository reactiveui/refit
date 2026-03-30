![Refit](refit_logo.png)

## Refit: The automatic type-safe REST library for .NET Core, Xamarin and .NET

[![Build](https://github.com/reactiveui/refit/actions/workflows/ci-build.yml/badge.svg)](https://github.com/reactiveui/refit/actions/workflows/ci-build.yml) [![codecov](https://codecov.io/github/reactiveui/refit/branch/main/graph/badge.svg?token=2guEgHsDU2)](https://codecov.io/github/reactiveui/refit)

||Refit|Refit.HttpClientFactory|Refit.Newtonsoft.Json|
|-|-|-|-|
|*NuGet*|[![NuGet](https://img.shields.io/nuget/v/Refit.svg)](https://www.nuget.org/packages/Refit/)|[![NuGet](https://img.shields.io/nuget/v/Refit.HttpClientFactory.svg)](https://www.nuget.org/packages/Refit.HttpClientFactory/)|[![NuGet](https://img.shields.io/nuget/v/Refit.Newtonsoft.Json.svg)](https://www.nuget.org/packages/Refit.Newtonsoft.Json/)|

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
.NET Core supports registering via HttpClientFactory
```csharp
services
    .AddRefitClient<IGitHubApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.github.com"));
```

# Table of Contents

* [Where does this work?](#where-does-this-work)
  * [Breaking changes in 6.x](#breaking-changes-in-6x)
  * [Breaking changes in 11.x](#breaking-changes-in-11x)
* [API Attributes](#api-attributes)
* [Querystrings](#querystrings)
  * [Dynamic Querystring Parameters](#dynamic-querystring-parameters)
  * [Collections as Querystring parameters](#collections-as-querystring-parameters)
  * [Unescape Querystring parameters](#unescape-querystring-parameters)
  * [Custom Querystring Parameter formatting](#custom-querystring-parameter-formatting)
* [Body content](#body-content)
  * [Buffering and the Content-Length header](#buffering-and-the-content-length-header)
  * [JSON content](#json-content)
  * [XML Content](#xml-content)
  * [Form posts](#form-posts)
* [Setting request headers](#setting-request-headers)
  * [Static headers](#static-headers)
  * [Dynamic headers](#dynamic-headers)
  * [Bearer Authentication](#bearer-authentication)
  * [Reducing header boilerplate with DelegatingHandlers (Authorization headers worked example)](#reducing-header-boilerplate-with-delegatinghandlers-authorization-headers-worked-example)
  * [Redefining headers](#redefining-headers)
  * [Removing headers](#removing-headers)
* [Passing state into DelegatingHandlers](#passing-state-into-delegatinghandlers)
  * [Support for Polly and Polly.Context](#support-for-polly-and-pollycontext)
  * [Target Interface type](#target-interface-type)
  * [MethodInfo of the method on the Refit client interface that was invoked](#methodinfo-of-the-method-on-the-refit-client-interface-that-was-invoked)
* [Multipart uploads](#multipart-uploads)
* [Retrieving the response](#retrieving-the-response)
* [Using generic interfaces](#using-generic-interfaces)
* [Interface inheritance](#interface-inheritance)
  * [Headers inheritance](#headers-inheritance)
* [Default Interface Methods](#default-interface-methods)
* [Using HttpClientFactory](#using-httpclientfactory)
* [Providing a custom HttpClient](#providing-a-custom-httpclient)
* [Handling exceptions](#handling-exceptions)
  * [When returning Task&lt;IApiResponse&gt;, Task&lt;IApiResponse&lt;T&gt;&gt;, or Task&lt;ApiResponse&lt;T&gt;&gt;](#when-returning-taskiapiresponse-taskiapiresponset-or-taskapiresponset)
  * [When returning Task&lt;T&gt;](#when-returning-taskt)
  * [Providing a custom ExceptionFactory](#providing-a-custom-exceptionfactory)
  * [ApiException deconstruction with Serilog](#apiexception-deconstruction-with-serilog)

### Sponsers

Refit is sponsored by the following:

<table>
  <tbody>
    <tr>
      <td align="center" valign="center">
        <a href="https://lombiq.com/">
        <svg xmlns="http://www.w3.org/2000/svg" width="400" viewBox="0 0 708.7 290.1">
            <style type="text/css">
	            .st0{fill-rule:evenodd;clip-rule:evenodd;fill:#ED4B6A;}
	            .st1{fill:#414042;}
	            .st2{fill-rule:evenodd;clip-rule:evenodd;fill:#42B771;}
	            .st3{fill-rule:evenodd;clip-rule:evenodd;fill:#60B9CE;}
            </style>
            <g>
	            <path class="st0" d="M176.7,132.9l-15.1-26.1c0,0-0.8-1.4-2.6-1.4c-1.8,0-2.6,1.4-2.6,1.4l-15.1,26.1c0,0-0.7,1.3-0.4,3.7
		            c0.4,3,2.6,4.5,2.6,4.5l9.6,6c0,0,2.3,1.6,5.8,1.6c0,0,0.1,0,0.1,0c0,0,0.1,0,0.1,0c3.5,0,5.8-1.6,5.8-1.6l9.6-6
		            c0,0,2.2-1.5,2.6-4.5C177.5,134.2,176.7,132.9,176.7,132.9z"/>
	            <g>
		            <path class="st1" d="M301.2,118.4c-2.9-2.3-6.3-4.1-10.2-5.3c-4-1.2-8.2-1.8-12.6-1.8c-4.2,0-8.3,0.6-12.4,1.8
			            c-4.1,1.2-7.7,2.9-10.9,5.2c-3.2,2.3-5.9,5.2-7.9,8.6c-2.1,3.5-3.2,7.5-3.5,12.2l-1.1,21.8c-0.2,4.7,0.5,8.7,2.1,12.2
			            c1.6,3.5,4,6.3,6.9,8.6c3,2.3,6.4,4,10.3,5.1c3.9,1.1,8,1.7,12.3,1.7c4.2,0,8.3-0.6,12.3-1.7c4-1.1,7.6-2.8,10.9-5.1
			            c3.2-2.3,5.9-5.2,7.9-8.6c2.1-3.5,3.2-7.5,3.5-12.2l1.1-21.5c0.2-4.7-0.4-8.8-1.9-12.3C306.3,123.7,304.1,120.7,301.2,118.4z
			             M290.3,142.4l-0.8,15c-0.1,2.4-0.6,4.5-1.5,6.2c-0.9,1.7-2,3.1-3.4,4.1c-1.4,1.1-2.9,1.8-4.5,2.3c-1.6,0.5-3.2,0.7-4.8,0.7
			            c-1.8,0-3.5-0.2-5.1-0.7c-1.6-0.5-3-1.2-4.3-2.3c-1.3-1.1-2.2-2.4-2.9-4.1c-0.7-1.7-0.9-3.7-0.8-6.2l0.8-15
			            c0.1-2.4,0.6-4.5,1.5-6.1c0.9-1.6,2-3,3.4-4.1c1.4-1.1,2.9-1.8,4.6-2.3c1.7-0.5,3.4-0.7,5-0.7c1.6,0,3.2,0.2,4.8,0.7
			            c1.5,0.5,3,1.2,4.2,2.3c1.3,1.1,2.3,2.4,3,4.1C290.1,137.9,290.4,139.9,290.3,142.4z"/>
		            <path class="st1" d="M535.9,81.1c-2.9,0-5.5,1.1-7.8,3.2c-2.2,2.1-3.4,4.7-3.6,7.8c-0.2,3,0.8,5.6,2.8,7.7c2,2.1,4.5,3.1,7.5,3.1
			            s5.5-1,7.8-3.1c2.2-2.1,3.4-4.6,3.6-7.7c0.2-3-0.8-5.6-2.8-7.8C541.3,82.1,538.8,81.1,535.9,81.1z"/>
		            <path class="st1" d="M223.7,169.8c-0.7-0.6-1.2-1.3-1.4-2.4c-0.2-1-0.3-2.4-0.2-4.1l4.2-81.1c0-0.5-0.1-1.5-0.9-2.4
			            c-0.9-1-2.5-1.2-3.2-1.3h-15l-4.6,88.5c-0.2,3.3-0.1,6.2,0.2,8.8c0.3,2.6,1.1,4.8,2.5,6.7c1.4,1.9,3.4,3.3,6,4.3
			            c2.6,1,6.2,1.5,10.8,1.5c1.4,0,2.9-0.1,4.4-0.2c1.5-0.1,3.2-0.3,4.9-0.6l0.6-16.5c-2.1,0-3.9-0.1-5.2-0.3
			            C225.5,170.8,224.4,170.4,223.7,169.8z"/>
		            <path class="st1" d="M487.3,111.4c-2.6,0-5.8,0.5-9.4,1.4c-3.6,0.9-8,2.8-13,5.6l0.9-16l1-19.7c0,0,0.2-1.5-0.9-2.7
			            c-1-1.1-3-1.3-3.5-1.3h-14.7l-5.5,105.5c0,0.5,0.2,1.5,0.9,2.3c1.2,1.3,3.6,1.3,3.6,1.3h13.1l1.8-6.1c4,2.3,7.8,4,11.3,5.1
			            c3.5,1.1,6.5,1.6,9.2,1.6c7.9,0,14.3-2.3,19.1-6.8c4.8-4.5,7.5-11.4,8-20.6l1.2-22.2c0.4-8.2-1.2-14.8-4.7-19.8
			            C502,113.9,495.9,111.4,487.3,111.4z M490.8,141.3l-0.9,17.7c-0.2,4.1-1.3,7-3.2,8.8c-1.9,1.8-4.2,2.7-6.8,2.7
			            c-1.2,0-2.6-0.2-4.2-0.5c-1.6-0.4-3.1-0.8-4.7-1.3c-1.6-0.5-3.2-1.1-4.7-1.8c-1.5-0.7-2.8-1.4-3.8-2.1l1.5-27.9
			            c3.2-1.9,6.5-3.6,9.6-5c3.2-1.4,6-2.1,8.4-2.1c2.6,0,4.8,0.8,6.6,2.5C490.3,134,491.1,137,490.8,141.3z"/>
		            <path class="st1" d="M540.1,112.5h-14.7l-1.7,33h0l-2,38.2c0,0,0,0,0,0.1l0,0.1c0,0.4,0,1.6,0.9,2.5c0.9,1,2.7,1.2,3.3,1.3h14.9
			            l3.7-71.2c0,0,0.2-1.5-0.9-2.7C542.6,112.5,540.1,112.5,540.1,112.5z"/>
		            <path class="st1" d="M623.1,113.8c-1.2-1.3-3.6-1.3-3.6-1.3h-13.1l-1.8,6.1c-4.1-2.5-7.9-4.4-11.4-5.5c-3.4-1.1-6.5-1.7-9.1-1.7
			            c-7.9,0-14.3,2.3-19.3,6.9c-4.9,4.6-7.6,11.5-8.1,20.7l-1.2,22.1c-0.4,8.2,1.3,14.8,5.1,19.8c3.8,5,9.9,7.5,18.3,7.5
			            c2.6,0,5.8-0.4,9.4-1.3c3.7-0.9,7.9-2.6,12.8-5.3l-1.7,32.6c0,0.5,0.2,1.4,0.9,2.3c1.2,1.3,3.6,1.3,3.6,1.3h14.7l0,0l3.3-63.2h0
			            l2-38.2C624,116.5,624.2,115,623.1,113.8z M602.2,163.4c-3.4,2-6.6,3.6-9.7,4.8c-3.1,1.2-5.8,1.8-8.2,1.8c-2.7,0-5-0.8-6.7-2.5
			            c-1.7-1.7-2.5-4.6-2.3-8.9l0.9-17.7c0.2-4.1,1.3-7,3.3-8.8c2-1.8,4.3-2.7,6.9-2.7c1.1,0,2.4,0.2,3.9,0.5c1.5,0.4,3.1,0.8,4.7,1.4
			            c1.6,0.6,3.1,1.2,4.6,1.9c1.5,0.7,2.8,1.4,4,2.1L602.2,163.4z"/>
		            <path class="st1" d="M408.3,111.4c-1.3,0-2.7,0.1-4.2,0.3c-1.5,0.2-3.2,0.7-5.2,1.4c-2,0.7-4.2,1.8-6.7,3.1
			            c-2.5,1.4-5.4,3.2-8.7,5.6c-1.6-3.2-3.9-5.8-6.9-7.6c-3.1-1.8-6.9-2.7-11.6-2.7c-1.3,0-2.6,0.1-4,0.2c-1.3,0.2-2.8,0.5-4.5,1.1
			            c-1.7,0.6-3.5,1.4-5.6,2.5c-2.1,1.1-4.4,2.5-7.1,4.2l-1.2-6.9h-17.8l-3.8,71.6c0,0.5,0.1,1.5,0.9,2.4c1.1,1.2,3.2,1.3,3.5,1.3
			            h14.7l2.7-51.2c3.7-2.4,6.7-4.1,9-5c2.3-0.9,4.7-1.4,7-1.4c2.2,0,4.2,0.8,5.8,2.3c1.6,1.5,2.4,4.4,2.1,8.5l-0.2,4.6h0l-2,38.2
			            c0,0-0.2,1.5,0.9,2.7c0.9,1.1,2.7,1.3,3.4,1.3h14.9l2.3-43.6c0.1-1.2,0.1-2.4,0.2-3.6c0.1-1.2,0.1-2.4,0-3.6
			            c3.6-2.5,6.6-4.3,8.9-5.3c2.4-1,4.7-1.5,6.9-1.5c2.7,0,4.8,0.8,6.4,2.4c1.6,1.6,2.3,4.5,2.1,8.5l-2.3,43c0,0.5,0.1,1.5,0.9,2.4
			            c0.9,1,2.6,1.2,3.3,1.3h15l2.6-48.8c0.4-8.2-1-14.9-4.4-20C422.3,113.9,416.5,111.4,408.3,111.4z"/>
	            </g>
	            <g>
		            <path class="st1" d="M172.9,97c0,0-0.2,6.5,0.3,8.5c0.5,2,3.6,7,3.6,7l12.4,22l2.5-47.3c0.3-4.7-3.4-8.5-8.1-8.6L173,78.6
			            L172.9,97z"/>
		            <path class="st1" d="M101.3,186.5c-1.4-2.5,0.6-5.5,0.6-5.5l38.7-66.8c0,0,3.4-5.5,4-7.7c0.6-2.3,0.4-9.5,0.4-9.5l-0.1-18.4h-40.8
			            l-5.9,101.5c-0.2,3.1,1.4,5.9,3.8,7.4C101.8,187.2,101.5,186.9,101.3,186.5z"/>
	            </g>
	            <path class="st1" d="M125.7,76.2"/>
	            <path class="st2" d="M131.7,149.8l-15,26.1c0,0-0.8,1.4,0.1,3c0.9,1.5,2.5,1.5,2.5,1.5l30.1,0c0,0,1.5,0,3.4-1.5
		            c2.4-1.8,2.6-4.5,2.6-4.5l0.4-11.3c0,0,0.2-2.8-1.5-5.8c0,0,0,0,0-0.1c0,0,0,0,0-0.1c-1.8-3-4.3-4.2-4.3-4.2l-10-5.4
		            c0,0-2.4-1.2-5.2,0C132.5,148.5,131.7,149.8,131.7,149.8z"/>
	            <path class="st3" d="M188.2,153.1l-1.9-3.3c0,0-0.7-1.3-3-2.2c-2.8-1.2-5.2,0-5.2,0l-10,5.4c0,0-2.5,1.2-4.3,4.2c0,0,0,0,0,0.1
		            c0,0,0,0,0,0.1c-1.7,3-1.5,5.8-1.5,5.8l0.4,11.3c0,0,0.2,2.7,2.6,4.5c1.9,1.5,3.4,1.5,3.4,1.5h18.2L188.2,153.1z"/>
            </g>
        </svg>
        </a>
      </td>
      <td align="center" valign="center">
        <a href="https://www.jetbrains.com/">
        <svg xmlns="http://www.w3.org/2000/svg" width="400" fill="none" viewBox="0 0 298 64">
          <defs>
            <linearGradient id="a" x1=".850001" x2="62.62" y1="62.72" y2="1.81" gradientUnits="userSpaceOnUse">
              <stop stop-color="#FF9419"/>
              <stop offset=".43" stop-color="#FF021D"/>
              <stop offset=".99" stop-color="#E600FF"/>
            </linearGradient>
          </defs>
          <path fill="#000" d="M86.4844 40.5858c0 .8464-.1792 1.5933-.5377 2.2505-.3585.6573-.8564 1.1651-1.5137 1.5236-.6572.3585-1.3941.5378-2.2406.5378H78v6.1044h5.0787c1.912 0 3.6248-.4282 5.1484-1.2846 1.5236-.8564 2.7186-2.0415 3.585-3.5452.8663-1.5037 1.3045-3.1966 1.3045-5.0886V21.0178h-6.6322v19.568Zm17.8556-1.8224h13.891v-5.6065H104.34v-6.3633h15.355v-5.7758H97.8766v29.9743h22.2464v-5.7757H104.34v-6.453Zm17.865-11.8005h8.882v24.0193h6.633V26.9629h8.842v-5.9451h-24.367v5.9551l.01-.01Zm47.022 9.0022c-.517-.2788-1.085-.4879-1.673-.6472.449-.1295.877-.2888 1.275-.488 1.096-.5676 1.962-1.3643 2.579-2.39.618-1.0257.936-2.2007.936-3.5351 0-1.5237-.418-2.8879-1.244-4.0929-.827-1.195-1.992-2.131-3.486-2.8082-1.494-.6672-3.206-1.0058-5.118-1.0058h-13.315v29.9743h13.574c2.011 0 3.804-.3485 5.387-1.0556 1.573-.707 2.798-1.6829 3.675-2.9476.866-1.2547 1.304-2.6887 1.304-4.302 0-1.4837-.338-2.8082-1.026-3.9833-.687-1.175-1.633-2.0812-2.858-2.7285l-.01.0099Zm-13.603-9.9184h5.886c.816 0 1.533.1494 2.161.4382.627.2888 1.115.707 1.464 1.2547.348.5378.527 1.1751.527 1.9021 0 .7269-.179 1.414-.527 1.9817-.349.5676-.837.9958-1.464 1.3045-.628.3087-1.345.4581-2.161.4581h-5.886v-7.3492.0099Zm10.138 18.134c-.378.5676-.916 1.0058-1.603 1.3145-.697.3087-1.484.4581-2.39.4581h-6.145v-7.6878h6.145c.886 0 1.673.1693 2.37.4979.687.3286 1.235.7867 1.613 1.3842.378.5975.578 1.2747.578 2.0414 0 .7668-.19 1.4241-.568 1.9917Zm29.596-5.3077c1.663-.7967 2.947-1.922 3.864-3.3659.916-1.444 1.374-3.117 1.374-5.0289 0-1.912-.448-3.5253-1.344-4.9592-.897-1.434-2.171-2.5394-3.814-3.3261-1.644-.7867-3.546-1.1751-5.717-1.1751h-13.124v29.9743h6.642V40.0779h4.322l6.084 10.9142h7.578l-6.851-11.7208c.339-.1195.677-.249.996-.3983h-.01Zm-2.151-6.1244c-.369.6274-.896 1.1154-1.583 1.444-.688.3386-1.494.5079-2.42.5079h-5.975v-8.2953h5.975c.926 0 1.732.1693 2.42.4979.687.3287 1.214.8166 1.583 1.434.368.6174.558 1.3544.558 2.1908 0 .8365-.19 1.5734-.558 2.2008v.0199Zm20.594-11.7308-10.706 29.9743h6.742l2.121-6.6122h11.114l2.27 6.6122h6.612L220.99 21.0178h-7.189Zm-.339 18.3431 3.445-10.5756.409-1.922.408 1.922 3.685 10.5756h-7.947Zm20.693 11.6312h6.851V21.0178h-6.851v29.9743Zm31.02-9.6993-12.896-20.275h-6.463v29.9743h6.055V30.7172l12.826 20.2749h6.533V21.0178h-6.055v20.275Zm31.528-3.3559c-.647-1.2448-1.564-2.2904-2.729-3.1369-1.165-.8464-2.509-1.4041-4.023-1.6929l-5.098-1.0456c-.797-.1892-1.434-.5178-1.902-.9958-.469-.478-.708-1.0755-.708-1.7825 0-.6473.17-1.205.518-1.683.339-.478.827-.8464 1.444-1.1153.618-.2689 1.335-.3983 2.151-.3983.817 0 1.554.1394 2.181.4182.627.2788 1.115.6672 1.464 1.1751s.528 1.0755.528 1.7228h6.642c-.04-1.7427-.528-3.2863-1.444-4.6207-.916-1.3443-2.201-2.3899-3.834-3.1468-1.633-.7568-3.505-1.1352-5.597-1.1352-2.091 0-3.943.3884-5.566 1.1751-1.623.7867-2.898 1.8721-3.804 3.2663-.906 1.3941-1.364 2.9775-1.364 4.76 0 1.444.288 2.7485.876 3.9036.587 1.1652 1.414 2.1311 2.479 2.8979 1.076.7668 2.311 1.3045 3.725 1.6033l5.397 1.1153c.886.2091 1.584.5975 2.101 1.1551.518.5577.767 1.2448.767 2.0813 0 .6672-.189 1.2747-.567 1.8025-.379.5277-.907.936-1.584 1.2248-.677.2888-1.474.4282-2.39.4282-.916 0-1.782-.1593-2.529-.478-.747-.3186-1.325-.7767-1.733-1.3742-.418-.5875-.617-1.2747-.617-2.0414h-6.642c.029 1.8721.527 3.5152 1.513 4.9492.976 1.424 2.32 2.5394 4.033 3.336 1.713.7967 3.675 1.195 5.886 1.195 2.21 0 4.202-.4083 5.915-1.2249 1.723-.8165 3.057-1.9418 4.023-3.3758.966-1.434 1.444-3.0572 1.444-4.8696 0-1.4838-.329-2.848-.976-4.1028l.02.01Z"/>
          <path fill="url(#a)" d="M20.34 3.66 3.66 20.34C1.32 22.68 0 25.86 0 29.18V59c0 2.76 2.24 5 5 5h29.82c3.32 0 6.49-1.32 8.84-3.66l16.68-16.68c2.34-2.34 3.66-5.52 3.66-8.84V5c0-2.76-2.24-5-5-5H29.18c-3.32 0-6.49 1.32-8.84 3.66Z"/>
          <path fill="#000" d="M48 16H8v40h40V16Z"/>
          <path fill="#fff" d="M30 47H13v4h17v-4Z"/>
        </svg>
        </a>
      </td>
      <td align="center" valign="center">
        <a href="https://claude.com/">
        <svg xmlns="http://www.w3.org/2000/svg" width="400" viewBox="0 0 573 125" fill="none"><path d="M200.168 110.625C190.376 110.625 181.647 108.688 173.98 104.813C166.355 100.896 160.397 95.4167 156.105 88.375C151.814 81.3333 149.668 73.25 149.668 64.125C149.668 54.4167 151.855 45.7917 156.23 38.25C160.647 30.7083 166.751 24.8542 174.543 20.6875C182.335 16.4792 191.189 14.375 201.105 14.375C207.064 14.375 213.001 15.0208 218.918 16.3125C224.876 17.5625 230.105 19.5208 234.605 22.1875V42.75H228.98C227.397 35.2083 224.293 29.7292 219.668 26.3125C215.085 22.8958 208.814 21.1875 200.855 21.1875C193.23 21.1875 186.897 22.8958 181.855 26.3125C176.814 29.7292 173.085 34.5 170.668 40.625C168.251 46.7083 167.043 53.7917 167.043 61.875C167.043 69.8333 168.397 76.9792 171.105 83.3125C173.814 89.6458 177.835 94.6458 183.168 98.3125C188.501 101.979 194.918 103.813 202.418 103.813C207.501 103.813 211.855 102.813 215.48 100.813C219.147 98.8125 222.23 96.0833 224.73 92.625C227.23 89.125 229.564 84.8333 231.73 79.75H237.605L233.605 102.313C229.272 105.104 224.105 107.188 218.105 108.563C212.105 109.938 206.126 110.625 200.168 110.625ZM243.168 103.938C245.626 103.646 247.543 103.271 248.918 102.813C250.335 102.313 251.355 101.646 251.98 100.813C252.605 99.9792 252.918 98.9167 252.918 97.625V29.5625L243.168 25.875V21.6875L262.793 14.375H267.793V97.625C267.793 98.9167 268.105 99.9792 268.73 100.813C269.355 101.646 270.355 102.313 271.73 102.813C273.147 103.271 275.085 103.646 277.543 103.938V109.375H243.168V103.938ZM300.355 110.625C296.772 110.625 293.605 109.958 290.855 108.625C288.105 107.292 285.96 105.417 284.418 103C282.918 100.583 282.168 97.7917 282.168 94.625C282.168 90 283.626 86.1875 286.543 83.1875C289.501 80.1458 294.043 77.75 300.168 76L322.855 69.5625V62.75C322.855 58.2917 321.793 54.9167 319.668 52.625C317.585 50.3333 314.48 49.1875 310.355 49.1875C306.73 49.1875 303.855 50.2917 301.73 52.5C299.647 54.7083 298.605 57.7083 298.605 61.5V67.125H288.48C287.272 66.375 286.335 65.3958 285.668 64.1875C285.043 62.9375 284.73 61.5625 284.73 60.0625C284.73 57.1042 285.876 54.3958 288.168 51.9375C290.46 49.4375 293.564 47.4583 297.48 46C301.397 44.5417 305.689 43.8125 310.355 43.8125C316.189 43.8125 321.147 44.6875 325.23 46.4375C329.314 48.1875 332.418 50.7708 334.543 54.1875C336.668 57.6042 337.73 61.7292 337.73 66.5625V96.25C337.73 97.7083 338.022 98.875 338.605 99.75C339.23 100.625 340.23 101.333 341.605 101.875C343.022 102.375 344.98 102.771 347.48 103.063V108.5C343.855 109.792 340.376 110.438 337.043 110.438C333.001 110.438 329.751 109.479 327.293 107.563C324.876 105.646 323.439 102.896 322.98 99.3125C319.939 103.063 316.522 105.896 312.73 107.813C308.939 109.688 304.814 110.625 300.355 110.625ZM307.668 100.625C310.335 100.625 312.98 100 315.605 98.75C318.272 97.4583 320.689 95.6667 322.855 93.375V75.3125L305.855 80.375C302.939 81.25 300.71 82.625 299.168 84.5C297.626 86.3333 296.855 88.5833 296.855 91.25C296.855 93.0833 297.314 94.7083 298.23 96.125C299.147 97.5417 300.418 98.6458 302.043 99.4375C303.71 100.229 305.585 100.625 307.668 100.625ZM376.543 110.625C369.876 110.625 364.814 108.938 361.355 105.563C357.897 102.146 356.168 97.1667 356.168 90.625V58.375L346.418 54.9375V50.75L366.105 43.8125H371.043V88.0625C371.043 92.0208 372.043 94.9583 374.043 96.875C376.043 98.7917 379.126 99.75 383.293 99.75C385.96 99.75 388.814 99.1458 391.855 97.9375C394.939 96.7292 397.814 95.0625 400.48 92.9375V58.375L390.73 54.9375V50.75L410.418 43.8125H415.355V92.5C415.355 93.9583 415.647 95.125 416.23 96C416.855 96.875 417.855 97.5625 419.23 98.0625C420.605 98.5625 422.564 98.9792 425.105 99.3125V104.75L405.418 110H400.48V98.9375C396.98 102.563 393.085 105.417 388.793 107.5C384.543 109.583 380.46 110.625 376.543 110.625ZM458.73 110.625C453.105 110.625 448.043 109.354 443.543 106.813C439.085 104.229 435.585 100.688 433.043 96.1875C430.501 91.6458 429.23 86.5625 429.23 80.9375C429.23 73.6042 430.751 67.125 433.793 61.5C436.876 55.875 441.189 51.5208 446.73 48.4375C452.272 45.3542 458.689 43.8125 465.98 43.8125C468.355 43.8125 470.772 44.0625 473.23 44.5625C475.73 45.0625 478.085 45.7708 480.293 46.6875V29.5625L470.543 25.875V21.6875L490.168 14.375H495.168V92.5C495.168 93.9583 495.46 95.125 496.043 96C496.668 96.875 497.668 97.5625 499.043 98.0625C500.418 98.5625 502.376 98.9792 504.918 99.3125V104.75L485.23 110H480.293V101.438C477.168 104.396 473.751 106.667 470.043 108.25C466.335 109.833 462.564 110.625 458.73 110.625ZM464.855 100.563C467.355 100.563 469.96 100.042 472.668 99C475.376 97.9167 477.918 96.4583 480.293 94.625V56C476.21 52.6667 471.751 51 466.918 51C462.168 51 458.126 52.125 454.793 54.375C451.46 56.625 448.939 59.7083 447.23 63.625C445.564 67.5417 444.73 71.9792 444.73 76.9375C444.73 81.6458 445.48 85.7708 446.98 89.3125C448.48 92.8542 450.73 95.625 453.73 97.625C456.772 99.5833 460.48 100.563 464.855 100.563ZM541.293 110.625C535.168 110.625 529.647 109.229 524.73 106.438C519.814 103.646 515.96 99.7708 513.168 94.8125C510.418 89.8125 509.043 84.1875 509.043 77.9375C509.043 71.6042 510.46 65.8333 513.293 60.625C516.126 55.4167 520.001 51.3125 524.918 48.3125C529.876 45.3125 535.376 43.8125 541.418 43.8125C546.001 43.8125 550.272 44.7708 554.23 46.6875C558.189 48.6042 561.501 51.2917 564.168 54.75C566.876 58.2083 568.668 62.1667 569.543 66.625L524.168 80.375C525.418 85.875 527.897 90.1875 531.605 93.3125C535.355 96.3958 539.96 97.9375 545.418 97.9375C550.001 97.9375 554.105 96.8542 557.73 94.6875C561.355 92.4792 564.564 89.1458 567.355 84.6875L572.168 86.1875C571.001 91.1042 568.939 95.4167 565.98 99.125C563.064 102.792 559.48 105.625 555.23 107.625C550.98 109.625 546.335 110.625 541.293 110.625ZM553.293 64.75C552.71 61.9583 551.751 59.5208 550.418 57.4375C549.126 55.3125 547.501 53.6875 545.543 52.5625C543.585 51.3958 541.397 50.8125 538.98 50.8125C535.939 50.8125 533.231 51.7083 530.856 53.5C528.481 55.2917 526.626 57.8333 525.293 61.125C523.96 64.375 523.293 68.1458 523.293 72.4375C523.293 73.1458 523.314 73.7083 523.355 74.125L553.293 64.75Z" fill="currentColor"></path><path d="M54.375 118.75L56.125 111L58.125 101L59.75 93L61.25 83.125L62.125 79.875L62 79.625L61.375 79.75L53.875 90L42.5 105.375L33.5 114.875L31.375 115.75L27.625 113.875L28 110.375L30.125 107.375L42.5 91.5L50 81.625L54.875 76L54.75 75.25H54.5L21.5 96.75L15.625 97.5L13 95.125L13.375 91.25L14.625 90L24.5 83.125L49.125 69.375L49.5 68.125L49.125 67.5H47.875L43.75 67.25L29.75 66.875L17.625 66.375L5.75 65.75L2.75 65.125L0 61.375L0.25 59.5L2.75 57.875L6.375 58.125L14.25 58.75L26.125 59.5L34.75 60L47.5 61.375H49.5L49.75 60.5L49.125 60L48.625 59.5L36.25 51.25L23 42.5L16 37.375L12.25 34.75L10.375 32.375L9.625 27.125L13 23.375L17.625 23.75L18.75 24L23.375 27.625L33.25 35.25L46.25 44.875L48.125 46.375L49 45.875V45.5L48.125 44.125L41.125 31.375L33.625 18.375L30.25 13L29.375 9.75C29.0417 8.625 28.875 7.375 28.875 6L32.75 0.750006L34.875 0L40.125 0.750006L42.25 2.625L45.5 10L50.625 21.625L58.75 37.375L61.125 42.125L62.375 46.375L62.875 47.75H63.75V47L64.375 38L65.625 27.125L66.875 13.125L67.25 9.125L69.25 4.375L73.125 1.87501L76.125 3.25L78.625 6.875L78.25 9.125L76.875 18.75L73.875 33.875L72 44.125H73.125L74.375 42.75L79.5 36L88.125 25.25L91.875 21L96.375 16.25L99.25 14H104.625L108.5 19.875L106.75 26L101.25 33L96.625 38.875L90 47.75L86 54.875L86.375 55.375H87.25L102.125 52.125L110.25 50.75L119.75 49.125L124.125 51.125L124.625 53.125L122.875 57.375L112.625 59.875L100.625 62.25L82.75 66.5L82.5 66.625L82.75 67L90.75 67.75L94.25 68H102.75L118.5 69.125L122.625 71.875L125 75.125L124.625 77.75L118.25 80.875L109.75 78.875L89.75 74.125L83 72.5H82V73L87.75 78.625L98.125 88L111.25 100.125L111.875 103.125L110.25 105.625L108.5 105.375L97 96.625L92.5 92.75L82.5 84.375H81.875V85.25L84.125 88.625L96.375 107L97 112.625L96.125 114.375L92.875 115.5L89.5 114.875L82.25 104.875L74.875 93.5L68.875 83.375L68.25 83.875L64.625 121.625L63 123.5L59.25 125L56.125 122.625L54.375 118.75Z" fill="#c96442"></path></svg>
        </a>
      </td>
    </tr>
  </tbody>
</table>


### Where does this work?

Refit currently supports the following platforms and any .NET Standard 2.0 target:

* WinUI
* Desktop .NET Framework 4.6.2+
* .NET 8 / 9 / 10
* Blazor
* Uno Platform

### SDK Requirements

### Updates in 8.0.x
Fixes for some issues experienced, this lead to some breaking changes.
See [Releases](https://github.com/reactiveui/refit/releases) for full details.

### V6.x.x

Refit 6 requires Visual Studio 16.8 or higher, or the .NET SDK 5.0.100 or higher. It can target any .NET Standard 2.0 platform.

Refit 6 does not support the old `packages.config` format for NuGet references (as they do not support analyzers/source generators). You must
[migrate to PackageReference](https://devblogs.microsoft.com/nuget/migrate-packages-config-to-package-reference/) to use Refit v6 and later.

#### Breaking changes in 6.x

Refit 6 makes [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview) the default JSON serializer. If you'd like to continue to use `Newtonsoft.Json`, add the `Refit.Newtonsoft.Json` NuGet package and set your `ContentSerializer` to `NewtonsoftJsonContentSerializer` on your `RefitSettings` instance. `System.Text.Json` is faster and uses less memory, though not all features are supported. The [migration guide](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to?pivots=dotnet-5-0) contains more details.

`IContentSerializer` was renamed to `IHttpContentSerializer` to better reflect its purpose. Additionally, two of its methods were renamed, `SerializeAsync<T>` -> `ToHttpContent<T>` and `DeserializeAsync<T>` -> `FromHttpContentAsync<T>`. Any existing implementations of these will need to be updated, though the changes should be minor.

##### Updates in 6.3

Refit 6.3 splits out the XML serialization via `XmlContentSerializer` into a separate package, `Refit.Xml`. This
is to reduce the dependency size when using Refit with Web Assembly (WASM) applications. If you require XML, add a reference
to `Refit.Xml`.

### V11.x.x

#### Breaking changes in 11.x

Refit 10 introduces `ApiRequestException` to represent requests that fail before receiving a response from the server.
This exception will now wrap previous exceptions such as `HttpRequestException` and `TaskCanceledException` when they occur during request execution.

* If you were not wrapping responses with `IApiResponse` and were catching these exceptions directly, you will need to update your code to catch `ApiRequestException` instead.
* If you were wrapping responses with `IApiResponse`, these exceptions will no longer be thrown and will instead be captured in the `IApiResponse.Error` property.
You can use the new `IApiResponse.HasRequestError(out var apiRequestException)` method to safely check and retrieve the `ApiRequestException` instance.

The `IApiResponse.Error` property's type has also changed to `ApiExceptionBase`, which is the new base class for `ApiException` and `ApiRequestException`.
If your code accessed members specific to `ApiException` (i.e. anything related to the response from the server), you can use the new `IApiResponse.HasResponseError(out var apiException)` method to safely check and retrieve the `ApiException` instance.

All response-related properties of `IApiResponse` are now nullable.
The new `IApiResponse.IsReceived` property can be used to check if a response was received from the server, and will mark those properties as non-null.
The original `IApiResponse.IsSuccessful` and `IApiResponse.IsSuccessStatusCode` properties can still be used to check if the response was received and is successful.

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
[Get("/group/{groupid}/users")]
Task<List<User>> GroupList(int groupId, [AliasAs("sort")] string sortOrder);

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

### Querystrings

#### Dynamic Querystring Parameters

If you specify an `object` as a query parameter, all public properties which are not null are used as query parameters.
This previously only applied to GET requests, but has now been expanded to all HTTP request methods, partly thanks to Twitter's hybrid API that insists on non-GET requests with querystring parameters.
Use the `Query` attribute to change the behavior to 'flatten' your query parameter object. If using this Attribute you can specify values for the Delimiter and the Prefix which are used to 'flatten' the object.

```csharp
public class MyQueryParams
{
    [AliasAs("order")]
    public string SortOrder { get; set; }

    public int Limit { get; set; }

    public KindOptions Kind { get; set; }
}

public enum KindOptions
{
    Foo,

    [EnumMember(Value = "bar")]
    Bar
}


[Get("/group/{id}/users")]
Task<List<User>> GroupList([AliasAs("id")] int groupId, MyQueryParams params);

[Get("/group/{id}/users")]
Task<List<User>> GroupListWithAttribute([AliasAs("id")] int groupId, [Query(".","search")] MyQueryParams params);


params.SortOrder = "desc";
params.Limit = 10;
params.Kind = KindOptions.Bar;

GroupList(4, params)
>>> "/group/4/users?order=desc&Limit=10&Kind=bar"

GroupListWithAttribute(4, params)
>>> "/group/4/users?search.order=desc&search.Limit=10&search.Kind=bar"
```

A similar behavior exists if using a Dictionary, but without the advantages of the `AliasAs` attributes and of course no intellisense and/or type safety.

You can also specify querystring parameters with [Query] and have them flattened in non-GET requests, similar to:
```csharp
[Post("/statuses/update.json")]
Task<Tweet> PostTweet([Query]TweetParams params);
```

Where `TweetParams` is a POCO, and properties will also support `[AliasAs]` attributes.

If you need to keep internal-only properties on your query DTO, mark them with one of the standard ignore attributes and Refit will skip them when building the query string:

- `[IgnoreDataMember]`
- `[System.Text.Json.Serialization.JsonIgnore]`
- `[Newtonsoft.Json.JsonIgnore]`

#### Collections as Querystring parameters

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

#### Unescape Querystring parameters

Use the `QueryUriFormat` attribute to specify if the query parameters should be url escaped

```csharp
[Get("/query")]
[QueryUriFormat(UriFormat.Unescaped)]
Task Query(string q);

Query("Select+Id,Name+From+Account")
>>> "/query?q=Select+Id,Name+From+Account"
```

#### Custom Querystring parameter formatting

**Formatting Keys**

To customize the format of query keys, you have two main options:

1. **Using the `AliasAs` Attribute**:

   You can use the `AliasAs` attribute to specify a custom key name for a property. This attribute will always take precedence over any key formatter you specify.

   ```csharp
   public class MyQueryParams
   {
       [AliasAs("order")]
       public string SortOrder { get; set; }

       public int Limit { get; set; }
   }

   [Get("/group/{id}/users")]
   Task<List<User>> GroupList([AliasAs("id")] int groupId, [Query] MyQueryParams params);

   params.SortOrder = "desc";
   params.Limit = 10;

   GroupList(1, params);
   ```

   This will generate the following request:

   ```
   /group/1/users?order=desc&Limit=10
   ```

2. **Using the `RefitSettings.UrlParameterKeyFormatter` Property**:

   By default, Refit uses the property name as the query key without any additional formatting. If you want to apply a custom format across all your query keys, you can use the `UrlParameterKeyFormatter` property. Remember that if a property has an `AliasAs` attribute, it will be used regardless of the formatter.

   The following example uses the built-in `CamelCaseUrlParameterKeyFormatter`:

   ```csharp
   public class MyQueryParams
   {
       public string SortOrder { get; set; }

       [AliasAs("queryLimit")]
       public int Limit { get; set; }
   }

   [Get("/group/users")]
   Task<List<User>> GroupList([Query] MyQueryParams params);

   params.SortOrder = "desc";
   params.Limit = 10;
   ```

   The request will look like:

   ```
   /group/users?sortOrder=desc&queryLimit=10
   ```

**Note**: The `AliasAs` attribute always takes the top priority. If both the attribute and a custom key formatter are present, the `AliasAs` attribute's value will be used.

#### Formatting URL Parameter Values with the `UrlParameterFormatter`

In Refit, the `UrlParameterFormatter` property within `RefitSettings` allows you to customize how parameter values are formatted in the URL. This can be particularly useful when you need to format dates, numbers, or other types in a specific manner that aligns with your API's expectations.

**Using `UrlParameterFormatter`**:

Assign a custom formatter that implements the `IUrlParameterFormatter` interface to the `UrlParameterFormatter` property.

```csharp
public class CustomDateUrlParameterFormatter : IUrlParameterFormatter
{
    public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type)
    {
        if (value is DateTime dt)
        {
            return dt.ToString("yyyyMMdd");
        }

        return value?.ToString();
    }
}

var settings = new RefitSettings
{
    UrlParameterFormatter = new CustomDateUrlParameterFormatter()
};
```

In this example, a custom formatter is created for date values. Whenever a `DateTime` parameter is encountered, it formats the date as `yyyyMMdd`.

**Formatting Dictionary Keys**:

When dealing with dictionaries, it's important to note that keys are treated as values. If you need custom formatting for dictionary keys, you should use the `UrlParameterFormatter` as well.

For instance, if you have a dictionary parameter and you want to format its keys in a specific way, you can handle that in the custom formatter:

```csharp
public class CustomDictionaryKeyFormatter : IUrlParameterFormatter
{
    public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type)
    {
        // Handle dictionary keys
        if (attributeProvider is PropertyInfo prop && prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            // Custom formatting logic for dictionary keys
            return value?.ToString().ToUpperInvariant();
        }

        return value?.ToString();
    }
}

var settings = new RefitSettings
{
    UrlParameterFormatter = new CustomDictionaryKeyFormatter()
};
```

In the above example, the dictionary keys will be converted to uppercase.

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

JSON requests and responses are serialized/deserialized using an instance of the `IHttpContentSerializer` interface. Refit provides two implementations out of the box: `SystemTextJsonContentSerializer` (which is the default JSON serializer) and `NewtonsoftJsonContentSerializer`. The first uses `System.Text.Json` APIs and is focused on high performance and low memory usage, while the latter uses the known `Newtonsoft.Json` library and is more versatile and customizable. You can read more about the two serializers and the main differences between the two [at this link](https://docs.microsoft.com/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to).

For instance, here is how to create a new `RefitSettings` instance using the `Newtonsoft.Json`-based serializer (you'll also need to add a `PackageReference` to `Refit.Newtonsoft.Json`):

```csharp
var settings = new RefitSettings(new NewtonsoftJsonContentSerializer());
```

If you're using `Newtonsoft.Json` APIs, you can customize their behavior by setting the `Newtonsoft.Json.JsonConvert.DefaultSettings` property:

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

##### JSON source generator

To apply the benefits of the new [JSON source generator](https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/) for System.Text.Json added in .NET 6, you can use `SystemTextJsonContentSerializer` with a custom instance of `RefitSettings` and `JsonSerializerOptions`:

```csharp
var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com",
    new RefitSettings {
        ContentSerializer = new SystemTextJsonContentSerializer(MyJsonSerializerContext.Default.Options)
    });
```

When using `System.Text.Json` polymorphism features such as `[JsonDerivedType]` / `[JsonPolymorphic]`, Refit serializes request bodies using the **declared Refit method parameter type** rather than the boxed runtime `object`. This ensures type discriminators configured on the base contract are preserved in outgoing request payloads.

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

Adding an `Authorization` header is such a common use case that you can add an access token to a request by applying an `Authorize` attribute to a parameter and optionally specifying the scheme:

```csharp
[Get("/users/{user}")]
Task<User> GetUser(string user, [Authorize("Bearer")] string token);

// Will add the header "Authorization: Bearer OAUTH-TOKEN}" to the request
var user = await GetUser("octocat", "OAUTH-TOKEN");

//note: the scheme defaults to Bearer if none provided
```

If you need to set multiple headers at runtime, you can add a `IDictionary<string, string>`
and apply a `HeaderCollection` attribute to the parameter and it will inject the headers into the request:

[//]: # ({% raw %})
```csharp

[Get("/users/{user}")]
Task<User> GetUser(string user, [HeaderCollection] IDictionary<string, string> headers);

var headers = new Dictionary<string, string> {{"Authorization","Bearer tokenGoesHere"}, {"X-Tenant-Id","123"}};
var user = await GetUser("octocat", headers);
```
[//]: # ({% endraw %})

#### Bearer Authentication

Most APIs need some sort of Authentication. The most common is OAuth Bearer authentication. A header is added to each request of the form: `Authorization: Bearer <token>`. Refit makes it easy to insert your logic to get the token however your app needs, so you don't have to pass a token into each method.

1. Add `[Headers("Authorization: Bearer")]` to the interface or methods which need the token.
2. Set `AuthorizationHeaderValueGetter` in the `RefitSettings` instance. Refit will call your delegate each time it needs to obtain the token, so it's a good idea for your mechanism to cache the token value for some period within the token lifetime.

`AuthorizationHeaderValueGetter` works whether you create clients with `RestService.For<T>("https://...")` or supply your own `HttpClient` via `RestService.For<T>(httpClient, settings)`. If your API methods accept a `CancellationToken`, that token is propagated to the getter delegate.

#### Reducing header boilerplate with DelegatingHandlers (Authorization headers worked example)
Although we make provisions for adding dynamic headers at runtime directly in Refit,
most use-cases would likely benefit from registering a custom `DelegatingHandler` in order to inject the headers as part of the `HttpClient` middleware pipeline
thus removing the need to add lots of `[Header]` or `[HeaderCollection]` attributes.

In the example above we are leveraging a `[HeaderCollection]` parameter to inject an `Authorization` and `X-Tenant-Id` header.
This is quite a common scenario if you are integrating with a 3rd party that uses OAuth2. While it's ok for the occasional endpoint,
it would be quite cumbersome if we had to add that boilerplate to every method in our interface.

In this example we will assume our application is a multi-tenant application that is able to pull information about a tenant through
some interface `ITenantProvider` and has a data store `IAuthTokenStore` that can be used to retrieve an auth token to attach to the outbound request.

```csharp

 //Custom delegating handler for adding Auth headers to outbound requests
 class AuthHeaderHandler : DelegatingHandler
 {
     private readonly ITenantProvider tenantProvider;
     private readonly IAuthTokenStore authTokenStore;

    public AuthHeaderHandler(ITenantProvider tenantProvider, IAuthTokenStore authTokenStore)
    {
         this.tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
         this.authTokenStore = authTokenStore ?? throw new ArgumentNullException(nameof(authTokenStore));
         // InnerHandler must be left as null when using DI, but must be assigned a value when
         // using RestService.For<IMyApi>
         // InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await authTokenStore.GetToken();

        //potentially refresh token here if it has expired etc.

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Tenant-Id", tenantProvider.GetTenantId());

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

//Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddTransient<ITenantProvider, TenantProvider>();
    services.AddTransient<IAuthTokenStore, AuthTokenStore>();
    services.AddTransient<AuthHeaderHandler>();

    //this will add our refit api implementation with an HttpClient
    //that is configured to add auth headers to all requests

    //note: AddRefitClient<T> requires a reference to Refit.HttpClientFactory
    //note: the order of delegating handlers is important and they run in the order they are added!

    services.AddRefitClient<ISomeThirdPartyApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"))
        .AddHttpMessageHandler<AuthHeaderHandler>();
        //you could add Polly here to handle HTTP 429 / HTTP 503 etc
}

//Your application code
public class SomeImportantBusinessLogic
{
    private ISomeThirdPartyApi thirdPartyApi;

    public SomeImportantBusinessLogic(ISomeThirdPartyApi thirdPartyApi)
    {
        this.thirdPartyApi = thirdPartyApi;
    }

    public async Task DoStuffWithUser(string username)
    {
        var user = await thirdPartyApi.GetUser(username);
        //do your thing
    }
}
```

If you aren't using dependency injection then you could achieve the same thing by doing something like this:

```csharp
var api = RestService.For<ISomeThirdPartyApi>(new HttpClient(new AuthHeaderHandler(tenantProvider, authTokenStore))
    {
        BaseAddress = new Uri("https://api.example.com")
    }
);

var user = await thirdPartyApi.GetUser(username);
//do your thing
```

#### Redefining headers

Unlike Retrofit, where headers do not overwrite each other and are all added to
the request regardless of how many times the same header is defined, Refit takes
a similar approach to the approach ASP.NET MVC takes with action filters &mdash;
**redefining a header will replace it**, in the following order of precedence:

* `Headers` attribute on the interface _(lowest priority)_
* `Headers` attribute on the method
* `Header` attribute or `HeaderCollection` attribute on a method parameter _(highest priority)_

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

### Passing state into DelegatingHandlers

If there is runtime state that you need to pass to a `DelegatingHandler` you can add a property with a dynamic value to the underlying `HttpRequestMessage.Properties`
by applying a `Property` attribute to a parameter:

```csharp
public interface IGitHubApi
{
    [Post("/users/new")]
    Task CreateUser([Body] User user, [Property("SomeKey")] string someValue);

    [Post("/users/new")]
    Task CreateUser([Body] User user, [Property] string someOtherKey);
}
```

The attribute constructor optionally takes a string which becomes the key in the `HttpRequestMessage.Properties` dictionary.
If no key is explicitly defined then the name of the parameter becomes the key.
If a key is defined multiple times the value in `HttpRequestMessage.Properties` will be overwritten.
The parameter itself can be any `object`. Properties can be accessed inside a `DelegatingHandler` as follows:

> ⚠️ **Important for `IHttpClientFactory` users:** `DelegatingHandler` instances are pooled and can live longer than a single request scope. Avoid reading per-request state from services that may be scoped/cached across handler lifetimes (for example a tenant/customer resolver stored on the handler). For per-request values like `CustomerId`, pass the value through `[Property]` so each request carries its own state.

```csharp
class RequestPropertyHandler : DelegatingHandler
{
    public RequestPropertyHandler(HttpMessageHandler innerHandler = null) : base(innerHandler ?? new HttpClientHandler()) {}

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // See if the request has a the property
        if(request.Properties.ContainsKey("SomeKey"))
        {
            var someProperty = request.Properties["SomeKey"];
            //do stuff
        }

        if(request.Properties.ContainsKey("someOtherKey"))
        {
            var someOtherProperty = request.Properties["someOtherKey"];
            //do stuff
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
```

Note: in .NET 5 `HttpRequestMessage.Properties` has been marked `Obsolete` and Refit will instead populate the value into the new `HttpRequestMessage.Options`.

#### Support for Polly and Polly.Context

Because Refit supports `HttpClientFactory` it is possible to configure Polly policies on your HttpClient.
If your policy makes use of `Polly.Context` this can be passed via Refit by adding `[Property("PolicyExecutionContext")] Polly.Context context`
as behind the scenes `Polly.Context` is simply stored in `HttpRequestMessage.Properties` under the key `PolicyExecutionContext` and is of type `Polly.Context`. It's only recommended to pass the `Polly.Context` this way if your use case requires that the `Polly.Context` be initialized with dynamic content only known at runtime. If your `Polly.Context` only requires the same content every time (e.g an `ILogger` that you want to use to log from inside your policies) a cleaner approach is to inject the `Polly.Context` via a `DelegatingHandler` as described in [#801](https://github.com/reactiveui/refit/issues/801#issuecomment-1137318526)

#### Target Interface Type and method info

There may be times when you want to know what the target interface type is of the Refit instance. An example is where you
have a derived interface that implements a common base like this:

```csharp
public interface IGetAPI<TEntity>
{
    [Get("/{key}")]
    Task<TEntity> Get(long key);
}

public interface IUsersAPI : IGetAPI<User>
{
}

public interface IOrdersAPI : IGetAPI<Order>
{
}
```

You can access the concrete type of the interface for use in a handler, such as to alter the URL of the request:

[//]: # ({% raw %})
```csharp
class RequestPropertyHandler : DelegatingHandler
{
    public RequestPropertyHandler(HttpMessageHandler innerHandler = null) : base(innerHandler ?? new HttpClientHandler()) {}

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get the type of the target interface
        Type interfaceType = (Type)request.Properties[HttpMessageRequestOptions.InterfaceType];

        var builder = new UriBuilder(request.RequestUri);
        // Alter the Path in some way based on the interface or an attribute on it
        builder.Path = $"/{interfaceType.Name}{builder.Path}";
        // Set the new Uri on the outgoing message
        request.RequestUri = builder.Uri;

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
```
[//]: # ({% endraw %})

The full method information (`RestMethodInfo`) is also always available in the request options. The `RestMethodInfo` contains more information about the method being called such as the full `MethodInfo` when using reflection is needed:

[//]: # ({% raw %})
```csharp
class RequestPropertyHandler : DelegatingHandler
{
    public RequestPropertyHandler(HttpMessageHandler innerHandler = null) : base(innerHandler ?? new HttpClientHandler()) {}

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get the method info
        if (request.Options.TryGetValue(new HttpRequestOptionsKey<RestMethodInfo>(HttpRequestMessageOptions.RestMethodInfo), out RestMethodInfo restMethodInfo))
        {
            var builder = new UriBuilder(request.RequestUri);
            // Alter the Path in some way based on the method info or an attribute on it
            builder.Path = $"/{restMethodInfo.MethodInfo.Name}{builder.Path}";
            // Set the new Uri on the outgoing message
            request.RequestUri = builder.Uri;
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
```
[//]: # ({% endraw %})

Note: in .NET 5 `HttpRequestMessage.Properties` has been marked `Obsolete` and Refit will instead populate the value into the new `HttpRequestMessage.Options`. Refit provides `HttpRequestMessageOptions.InterfaceType` and `HttpRequestMessageOptions.RestMethodInfo` to respectively access the interface type and REST method info from the options.

### Multipart uploads

Methods decorated with `Multipart` attribute will be submitted with multipart content type.
At this time, multipart methods support the following parameter types:

 - `string` (parameter name will be used as name and string value as value)
 - byte array
 - `Stream`
 - `FileInfo`

Name of the field in the multipart data priority precedence:

* `multipartItem.Name` if specified and not null (optional); dynamic, allows naming form data part at execution time.
* `[AliasAs]` attribute  (optional) that decorate the streamPart parameter in the method signature (see below); static, defined in code.
* `MultipartItem` parameter name (default) as defined in the method signature; static, defined in code.

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

To pass a `Stream` to this method, construct a StreamPart object like so:

```csharp
someApiInstance.UploadPhoto(id, new StreamPart(myPhotoStream, "photo.jpg", "image/jpeg"));
```

Note: The `AttachmentName` attribute that was previously described in this section has been deprecated and its use is not recommended.

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

There is also a generic wrapper class called `ApiResponse<T>` that can be used as a return type. Using this class as a return type allows you to retrieve not just the content as an object, but also any metadata associated with the request/response. This includes information such as response headers, the http status code and reason phrase (e.g. 404 Not Found), the response version, the original request message that was sent and in the case of an error, an `ApiException` object containing details of the error. Following are some examples of how you can retrieve the response metadata.

```csharp
//Returns the content within a wrapper class containing metadata about the request/response
[Get("/users/{user}")]
Task<ApiResponse<User>> GetUser(string user);

//Calling the API
var response = await gitHubApi.GetUser("octocat");

//Determining if a success status code was received and there wasn't any other error
//(for example, during content deserialization)
if(response.IsSuccessful)
{
    //YAY! Do the thing...
}

if (response.IsReceived)
{
    //Getting the status code (returns a value from the System.Net.HttpStatusCode enumeration)
    var httpStatus = response.StatusCode;

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
}
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

When using inheritance, existing header attributes will be passed along as well, and the inner-most ones will have precedence:

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

### Default Interface Methods
Starting with C# 8.0, default interface methods (a.k.a. DIMs) can be defined on interfaces. Refit interfaces can provide additional logic using DIMs, optionally combined with private and/or static helper methods:
```csharp
public interface IApiClient
{
    // implemented by Refit but not exposed publicly
    [Get("/get")]
    internal Task<string> GetInternal();
    // Publicly available with added logic applied to the result from the API call
    public async Task<string> Get()
        => FormatResponse(await GetInternal());
    private static String FormatResponse(string response)
        => $"The response is: {response}";
}
```
The type generated by Refit will implement the method `IApiClient.GetInternal`. If additional logic is required immediately before or after its invocation, it shouldn't be exposed directly and can thus be hidden from consumers by being marked as `internal`.
The default interface method `IApiClient.Get` will be inherited by all types implementing `IApiClient`, including - of course - the type generated by Refit.
Consumers of the `IApiClient` will call the public `Get` method and profit from the additional logic provided in its implementation (optionally, in this case, with the help of the private static helper `FormatResponse`).
To support runtimes without DIM-support (.NET Core 2.x and below or .NET Standard 2.0 and below), two additional types would be required for the same solution.
```csharp
internal interface IApiClientInternal
{
    [Get("/get")]
    Task<string> Get();
}
public interface IApiClient
{
    public Task<string> Get();
}
internal class ApiClient : IApiClient
{
    private readonly IApiClientInternal client;
    public ApiClient(IApiClientInternal client) => this.client = client;
    public async Task<string> Get()
        => FormatResponse(await client.Get());
    private static String FormatResponse(string response)
        => $"The response is: {response}";
}
```

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

// or injected from the container
services.AddRefitClient<IWebApi>(provider => new RefitSettings() { /* configure settings */ })
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

### Providing a custom HttpClient

You can supply a custom `HttpClient` instance by simply passing it as a parameter to the `RestService.For<T>` method:

```csharp
RestService.For<ISomeApi>(new HttpClient()
{
    BaseAddress = new Uri("https://www.someapi.com/api/")
});
```

However, when supplying a custom `HttpClient` instance, `HttpMessageHandlerFactory` will not be used because you already control the handler pipeline.

`AuthorizationHeaderValueGetter` does still work with `RestService.For<T>(httpClient, settings)` when the request includes an `Authorization` header placeholder (for example `[Headers("Authorization: Bearer")]`).

If you still want to be able to configure the `HtttpClient` instance that `Refit` provides while still making use of the above settings, simply expose the `HttpClient` on the API interface:

```csharp
interface ISomeApi
{
    // This will automagically be populated by Refit if the property exists
    HttpClient Client { get; }

    [Headers("Authorization: Bearer")]
    [Get("/endpoint")]
    Task<string> SomeApiEndpoint();
}
```

Then, after creating the REST service, you can set any `HttpClient` property you want, e.g. `Timeout`:

```csharp
SomeApi = RestService.For<ISomeApi>("https://www.someapi.com/api/", new RefitSettings()
{
    AuthorizationHeaderValueGetter = (rq, ct) => GetTokenAsync()
});

SomeApi.Client.Timeout = timeout;
```

### Native AoT / trimming guidance

Refit's recommended **source-generator-first** setup for Native AoT and trimmed applications is:

1. Use normal Refit interfaces so the Refit source generator produces the client implementation at build time.
2. Prefer `RestService.For<T>(...)` over reflection-heavy manual patterns around `Type` where possible.
3. Supply source-generated `System.Text.Json` metadata for your DTOs.

For the default `SystemTextJsonContentSerializer` on .NET 8+, Refit prefers `JsonTypeInfo` metadata from your configured `JsonSerializerOptions` when it is available. That means Native AoT apps can improve compatibility by supplying source-generated metadata through a `JsonSerializerContext` or `TypeInfoResolver` on the serializer options they pass into `SystemTextJsonContentSerializer`.

```csharp
[JsonSerializable(typeof(Todo))]
public partial class TodoJsonContext : JsonSerializerContext
{
}

var settings = new RefitSettings(
    new SystemTextJsonContentSerializer(
        new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = TodoJsonContext.Default
        }
    )
);

var api = RestService.For<ITodoApi>("https://api.example.com", settings);
```

If a generated Refit client cannot be found at runtime, Refit now explicitly points you back to the source generator/build output and recommends generated clients plus source-generated `System.Text.Json` metadata for Native AoT scenarios.

Refit also ships analyzers for newer Roslyn toolchains, including a Roslyn 5.0 build for newer Visual Studio versions.

### Handling exceptions
Refit has different exception handling behavior depending on if your Refit interface methods return `Task<T>` or if they return `Task<IApiResponse>`, `Task<IApiResponse<T>>`, or `Task<ApiResponse<T>>`.

#### <a id="when-returning-taskapiresponset"></a>When returning `Task<IApiResponse>`, `Task<IApiResponse<T>>`, or `Task<ApiResponse<T>>`
Refit traps any `HttpRequestException` or `TaskCanceledException` raised by the `HttpClient` in an `ApiRequestException`.
Refit also traps any `ApiException` raised by the `ExceptionFactory` when processing the response, and any errors that occur when attempting to deserialize the response to `ApiResponse<T>`.
In both cases, it will populate the exception into the `Error` property on `ApiResponse<T>` without throwing the exception.

You can then decide what to do like so:

```csharp
var response = await _myRefitClient.GetSomeStuff();
if(response.IsSuccessful)
{
   //do your thing
}
else
{
    // If you want to distinguish between request and response errors
    if (response.HasRequestError(out var requestError))
        _logger.LogError(requestError, "An error occurred while sending the request.");
    else if (response.HasResponseError(out var responseError))
        _logger.LogError(responseError, responseError.Content);

    // Or just log the error directly
    _logger.LogError(response.Error, "An error occurred while calling the API.");
}
```

> [!NOTE]
> The `IsSuccessful` property checks whether the response status code is in the range 200-299 and there wasn't any other error (for example, during content deserialization). If you just want to check the HTTP response status code, you can use the `IsSuccessStatusCode` property.

#### When returning `Task<T>`
Refit throws any exception raised by the `HttpClient` and wraps it in an `ApiRequestException`.
It also throws any `ApiException` raised by the `ExceptionFactory` when processing the response and any errors that occur when attempting to deserialize the response to `Task<T>`.

```csharp
// ...
try
{
   var result = await awesomeApi.GetFooAsync("bar");
}
catch (ApiRequestException exception)
{
    //exception handling for when a response was not received from the server
}
catch (ApiException exception)
{
   //exception handling for when a response was received from the server
}
// Or to not distinguish between request/response exceptions
catch (ApiExceptionBase exception)
{
   //exception handling for when an error occurs during the request/response
}
// ...
```

Refit can also throw `ValidationApiException` instead which in addition to the information present on `ApiException` also contains `ProblemDetails` when the service implements the [RFC 7807](https://tools.ietf.org/html/rfc7807) specification for problem details and the response content type is `application/problem+json`

For specific information on the problem details of the validation exception, simply catch `ValidationApiException`:

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

#### Providing a custom `ExceptionFactory`

You can also override default exceptions behavior that are raised by the `ExceptionFactory` when processing the result by providing a custom exception factory in `RefitSettings`. For example, you can suppress all `ApiException`s with the following:

```csharp
var nullTask = Task.FromResult<Exception>(null);

var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com",
    new RefitSettings {
        ExceptionFactory = httpResponse => nullTask;
    });
```

For exceptions raised when attempting to deserialize the response use DeserializationExceptionFactory described bellow.

#### Providing a custom `DeserializationExceptionFactory`

You can override default deserialization exceptions behavior that are raised by the `DeserializationExceptionFactory` when processing the result by providing a custom exception factory in `RefitSettings`. For example, you can suppress all deserialization exceptions with the following:

```csharp
var nullTask = Task.FromResult<Exception>(null);

var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com",
    new RefitSettings {
        DeserializationExceptionFactory = (httpResponse, exception) => nullTask;
    });
```

#### `ApiException` deconstruction with Serilog

For users of [Serilog](https://serilog.net), you can enrich the logging of `ApiException` using the
[Serilog.Exceptions.Refit](https://www.nuget.org/packages/Serilog.Exceptions.Refit) NuGet package. Details of how to
integrate this package into your applications can be found [here](https://github.com/RehanSaeed/Serilog.Exceptions#serilogexceptionsrefit).
