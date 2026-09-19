// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Checks settings whose effects occur while building or preparing a request.</summary>
internal static class SettingsPolicies
{
    /// <summary>The local transport shared by the policy scenarios.</summary>
    private static readonly BufferingHandler Handler = new();

    /// <summary>The client reused across all policy checks.</summary>
    private static readonly HttpClient Client = new(Handler) { BaseAddress = new("https://people.example") };

    /// <summary>Checks buffering precedence and unmatched-placeholder handling.</summary>
    /// <param name="serializer">The generated JSON serializer used by the other client examples.</param>
    /// <returns>Completion after each policy is checked.</returns>
    internal static async Task RunAsync(IHttpContentSerializer serializer)
    {
        await CheckBufferingAsync(serializer);
        await CheckUnmatchedAsync(serializer);
    }

    /// <summary>Compares the inherited policy with explicit body-attribute overrides.</summary>
    /// <param name="serializer">The JSON serializer with generated model metadata.</param>
    /// <returns>Completion after the transport observes all policy combinations.</returns>
    private static async Task CheckBufferingAsync(IHttpContentSerializer serializer)
    {
        BufferingHandler handler = Handler;
        HttpClient client = Client;
        foreach (bool buffer in new[] { false, true })
        {
            RefitSettings settings = new(serializer) { Buffered = buffer };
            ISettingsPolicyApi api = RestService.ForGenerated<ISettingsPolicyApi>(client, settings);
            SampleCheck.Equal(BufferingHandler.ReplyText, await api.InheritedAsync(new(1, "Ada")));
            SampleCheck.Equal(buffer, handler.LengthBeforeRead.HasValue);
            SampleCheck.Equal(BufferingHandler.ReplyText, await api.UnbufferedAsync(new(1, "Ada")));
            SampleCheck.Equal(false, handler.LengthBeforeRead.HasValue);
            SampleCheck.Equal(BufferingHandler.ReplyText, await api.BufferedAsync(new(1, "Ada")));
            SampleCheck.Equal(true, handler.LengthBeforeRead > 0);
        }
    }

    /// <summary>Checks rejection and preservation of a placeholder with no matching argument.</summary>
    /// <param name="serializer">The serializer retained by these client settings.</param>
    /// <returns>Completion after both path-building policies are checked.</returns>
    private static async Task CheckUnmatchedAsync(IHttpContentSerializer serializer)
    {
        HttpClient client = Client;
        foreach (bool allow in new[] { false, true })
        {
            RefitSettings settings = new(serializer) { AllowUnmatchedRouteParameters = allow };
            ISettingsPolicyApi api = RestService.ForGenerated<ISettingsPolicyApi>(client, settings);
            bool rejected = false;
            try
            {
                using HttpRequestMessage request = await api.UnmatchedAsync();
                SampleCheck.Equal("/policy/{tenant}", request.RequestUri?.OriginalString);
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            SampleCheck.Equal(!allow, rejected);
        }
    }
}
