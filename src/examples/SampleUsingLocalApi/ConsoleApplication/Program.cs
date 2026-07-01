// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using LibraryWithSDKandRefitService;
using Refit;

namespace ConsoleSampleUsingLocalApi;

/// <summary>The console application entry point that exercises the local Refit API sample.</summary>
internal static class Program
{
    /// <summary>Menu option for the GET request without parameters.</summary>
    private const int GetWithNoParameterOption = 1;

    /// <summary>Menu option for the GET request with an identifier.</summary>
    private const int GetWithParameterOption = 2;

    /// <summary>Menu option for the POST request.</summary>
    private const int PostOption = 3;

    /// <summary>Menu option for the PUT request.</summary>
    private const int PutOption = 4;

    /// <summary>Menu option for the DELETE request.</summary>
    private const int DeleteOption = 5;

    /// <summary>Sentinel option used when the input cannot be parsed.</summary>
    private const int InvalidOption = 6;

    /// <summary>Sample identifier used by the GET and PUT requests.</summary>
    private const int SampleId = 4;

    /// <summary>Sample identifier used by the DELETE request.</summary>
    private const int DeleteId = 5;

    /// <summary>Runs the interactive console loop that invokes the sample REST service.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Hello World!");
        using var client = new HttpClient { BaseAddress = new("http://localhost:61868") };
        var restApiService = RestService.For<IRestService>(client);
        Console.WriteLine(
            "Enter from the following numbers to access the APIs,\n1 for get ,\n2 for get with argument, \n3 for post,\n4 for put, \n5 for Delete \n");
        while (true)
        {
            switch (int.Parse(Console.ReadLine() ?? InvalidOption.ToString()))
            {
                case GetWithNoParameterOption:
                {
                    var result1 = await restApiService.GetWithNoParameterAsync().ConfigureAwait(false);
                    Console.WriteLine(result1);
                    break;
                }

                case GetWithParameterOption:
                {
                    var result2 = await restApiService.GetWithParameterAsync(SampleId).ConfigureAwait(false);
                    Console.WriteLine(result2);
                    break;
                }

                case PostOption:
                {
                    var result3 = await restApiService.PostWithTestObjectAsync(new()).ConfigureAwait(false);
                    Console.WriteLine(result3);
                    break;
                }

                case PutOption:
                {
                    var result4 = await restApiService.PutWithParametersAsync(SampleId, new()).ConfigureAwait(false);
                    Console.WriteLine(result4);
                    break;
                }

                case DeleteOption:
                {
                    var result5 = await restApiService.DeleteWithParametersAsync(DeleteId).ConfigureAwait(false);
                    Console.WriteLine(result5);
                    break;
                }

                default:
                {
                    Console.WriteLine("Bhai Please Enter valid if you are really serious");
                    break;
                }
            }
        }
    }
}
