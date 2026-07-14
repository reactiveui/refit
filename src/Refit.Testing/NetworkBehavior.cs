// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;

namespace Refit.Testing;

/// <summary>
/// Deterministic network-condition simulation for <see cref="StubHttp"/>, modelled on Retrofit's
/// <c>NetworkBehavior</c>. Attach one to a handler to inject latency, latency variance, simulated network
/// failures (a thrown <see cref="HttpRequestException"/>) and simulated HTTP errors (a non-2xx response),
/// so retry, timeout and <see cref="ApiException"/> handling can be exercised under test.
/// </summary>
/// <remarks>
/// All randomness is driven by a seeded <see cref="Random"/>, so a given seed reproduces the same sequence
/// of delays and faults within a runtime. Defaults mirror Retrofit: a 2 second delay, 40% variance, 3%
/// failure and 0% error. The calculation methods are usable standalone.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Security",
    "CA5394:Random is an insecure random number generator",
    Justification = "Deterministic, seeded pseudo-randomness is the required behavior for reproducible network-fault simulation; a cryptographic generator would defeat the seed.")]
public sealed class NetworkBehavior
{
    /// <summary>The default base delay, in seconds.</summary>
    private const double DefaultDelaySeconds = 2D;

    /// <summary>The default latency variance as a fraction of the delay.</summary>
    private const double DefaultVariance = 0.4D;

    /// <summary>The default probability that a request simulates a network failure.</summary>
    private const double DefaultFailurePercent = 0.03D;

    /// <summary>The width of the symmetric variance window applied to the delay.</summary>
    private const double VarianceSpan = 2D;

    /// <summary>Guards <see cref="_random"/>, which is not thread-safe.</summary>
    private readonly Lock _gate = new();

    /// <summary>The seeded random source driving delays and faults.</summary>
    private readonly Random _random;

    /// <summary>Initializes a new instance of the <see cref="NetworkBehavior"/> class with seed 0.</summary>
    public NetworkBehavior()
        : this(0)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="NetworkBehavior"/> class with the given seed.</summary>
    /// <param name="seed">The seed for the random source; the same seed reproduces the same sequence.</param>
    public NetworkBehavior(int seed)
    {
        _random = new(seed);
        Delay = TimeSpan.FromSeconds(DefaultDelaySeconds);
        Variance = DefaultVariance;
        FailurePercent = DefaultFailurePercent;
        ErrorStatusCode = HttpStatusCode.InternalServerError;
        FailureFactory = static () => new HttpRequestException("Refit.Testing simulated network failure.");
    }

    /// <summary>Gets or sets the base delay applied to each request. Defaults to 2 seconds.</summary>
    public TimeSpan Delay { get; set; }

    /// <summary>Gets or sets the latency variance as a fraction of <see cref="Delay"/> (0 = none). Defaults to 0.4.</summary>
    public double Variance { get; set; }

    /// <summary>Gets or sets the probability (0..1) that a request simulates a network failure. Defaults to 0.03.</summary>
    public double FailurePercent { get; set; }

    /// <summary>Gets or sets the probability (0..1) that a request simulates an HTTP error response. Defaults to 0.</summary>
    public double ErrorPercent { get; set; }

    /// <summary>Gets or sets the status code returned for a simulated HTTP error. Defaults to 500.</summary>
    public HttpStatusCode ErrorStatusCode { get; set; }

    /// <summary>Gets or sets the factory that produces the exception thrown for a simulated network failure.</summary>
    public Func<Exception> FailureFactory { get; set; }

    /// <summary>Calculates the next delay, applying the configured variance around <see cref="Delay"/>.</summary>
    /// <returns>The delay to apply to the next request.</returns>
    public TimeSpan NextDelay()
    {
        double sample;
        lock (_gate)
        {
            sample = _random.NextDouble();
        }

        var factor = 1D + (((sample * VarianceSpan) - 1D) * Variance);
        return TimeSpan.FromMilliseconds(Delay.TotalMilliseconds * Math.Max(0D, factor));
    }

    /// <summary>Determines whether the next request should simulate a network failure.</summary>
    /// <returns><see langword="true"/> when the request should fail.</returns>
    public bool NextIsFailure() => NextChance(FailurePercent);

    /// <summary>Determines whether the next request should simulate an HTTP error response.</summary>
    /// <returns><see langword="true"/> when the request should return an error status.</returns>
    public bool NextIsError() => NextChance(ErrorPercent);

    /// <summary>Creates the exception used to simulate a network failure.</summary>
    /// <returns>The exception to throw.</returns>
    public Exception CreateFailure() => FailureFactory();

    /// <summary>Creates the response used to simulate an HTTP error.</summary>
    /// <returns>A response with <see cref="ErrorStatusCode"/> and an empty body.</returns>
    public HttpResponseMessage CreateErrorResponse() =>
        new(ErrorStatusCode) { Content = new StringContent(string.Empty) };

    /// <summary>Draws the next random sample and compares it against a probability.</summary>
    /// <param name="percent">The probability (0..1) to test against.</param>
    /// <returns><see langword="true"/> when the sample falls under the probability.</returns>
    private bool NextChance(double percent)
    {
        lock (_gate)
        {
            return _random.NextDouble() < percent;
        }
    }
}
