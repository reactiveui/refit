using Xunit;
// ReSharper disable InconsistentNaming

namespace Refit.Tests;

public interface IManyCancellationTokens
{
    [Get("/")]
    Task<string> GetValue(CancellationToken token0, CancellationToken token1);
}

public interface IManyHeaderCollections
{
    [Get("/")]
    Task<string> GetValue([HeaderCollection] IDictionary<string, string> collection0, [HeaderCollection] IDictionary<string, string> collection1);
}

public interface IHeaderCollectionWrongType
{
    [Get("/")]
    Task<string> GetValue([HeaderCollection] IDictionary<string, object> collection);
}

public interface IDoesNotStartSlash
{
    [Get("users")]
    Task<string> GetValue();
}

public interface IUrlContainsCRLF
{
    [Get("/\r")]
    Task<string> GetValue();
}

public interface IRoundTripNotString
{
    [Get("/{**value}")]
    Task<string> GetValue(int value);
}

public interface IRoundTrippingLeadingWhitespace
{
    [Get("/{ **path}")]
    Task<string> GetValue(string path);
}

public interface IRoundTrippingTrailingWhitespace
{
    [Get("/{** path}")]
    Task<string> GetValue(string path);
}

public interface IInvalidParamSubstitution
{
    [Get("/{/path}")]
    Task<string> GetValue(string path);
}

public interface IUrlNoMatchingParameters
{
    [Get("/{value}")]
    Task<string> GetValue();
}

public interface IMultipartAndBody
{
    [Get("/}")]
    [Multipart]
    Task<string> GetValue([Body] string body);
}

public interface IManyBody
{
    [Get("/")]
    Task<string> GetValue([Body] string body0, [Body] string body1);
}

public class UserBody
{
    public string Value { get; set; }
}

public interface IManyComplexTypes
{
    [Post("/")]
    Task<string> PostValue(UserBody body0, UserBody body1);
}

public interface IManyAuthorize
{
    [Get("/")]
    Task<string> GetValue([Authorize("Bearer")] string token0, [Authorize("Bearer")] string token1);
}

public interface IInvalidReturnType
{
    [Get("/")]
    string GetValue();
}

public class RestServiceExceptionTests
{
    [Fact]
    public void ManyCancellationTokensShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(() => RestService.For<IManyCancellationTokens>("https://api.github.com"));
        AssertExceptionContains("only contain a single CancellationToken", exception);
    }

    [Fact]
    public void ManyHeaderCollectionShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(() => RestService.For<IManyHeaderCollections>("https://api.github.com"));
        AssertExceptionContains("Only one parameter can be a HeaderCollection parameter", exception);
    }

    [Fact]
    public void InvalidHeaderCollectionTypeShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(() => RestService.For<IHeaderCollectionWrongType>("https://api.github.com"));
        AssertExceptionContains("HeaderCollection parameter of type", exception);
    }

    [Fact]
    public void UrlDoesntStartWithSlashShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(() => RestService.For<IDoesNotStartSlash>("https://api.github.com"));
        AssertExceptionContains("must start with '/' and be of the form", exception);
    }

    [Fact]
    public void UrlContainsCRLFShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(() => RestService.For<IUrlContainsCRLF>("https://api.github.com"));
        AssertExceptionContains("must not contain CR or LF characters", exception);
    }

    [Fact]
    public void RoundTripParameterNotStringShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(() => RestService.For<IRoundTripNotString>("https://api.github.com"));
        AssertExceptionContains("has round-tripping parameter", exception);
    }

    [Fact]
    public void RoundTripWithLeadingWhitespaceShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(() => RestService.For<IRoundTrippingLeadingWhitespace>("https://api.github.com"));
        AssertExceptionContains("has parameter  **path, but no method parameter matches", exception);
    }

    [Fact]
    public void RoundTripWithTrailingWhitespaceShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(() => RestService.For<IRoundTrippingTrailingWhitespace>("https://api.github.com"));
        AssertExceptionContains("has parameter ** path, but no method parameter matches", exception);
    }

    [Fact]
    public void InvalidParamSubstitutionShouldNotThrow()
    {
        var service = RestService.For<IInvalidParamSubstitution>("https://api.github.com");
        Assert.NotNull(service);
    }

    [Fact]
    public void UrlNoMatchingParameterShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(() => RestService.For<IUrlNoMatchingParameters>("https://api.github.com"));
        AssertExceptionContains("but no method parameter matches", exception);
    }

    [Fact]
    public void MultipartAndBodyShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(() => RestService.For<IMultipartAndBody>("https://api.github.com"));
        AssertExceptionContains("Multipart requests may not contain a Body parameter", exception);
    }

    [Fact]
    public void ManyBodyShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(() => RestService.For<IManyBody>("https://api.github.com"));
        AssertExceptionContains("Only one parameter can be a Body parameter", exception);
    }

    [Fact]
    public void ManyComplexTypesShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(() => RestService.For<IManyComplexTypes>("https://api.github.com"));
        AssertExceptionContains("Multiple complex types found. Specify one parameter as the body using BodyAttribute", exception);
    }

    [Fact]
    public void ManyAuthorizeAttributesShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(() => RestService.For<IManyAuthorize>("https://api.github.com"));
        AssertExceptionContains("Only one parameter can be an Authorize parameter", exception);
    }

    [Fact]
    public void InvalidReturnTypeShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(() => RestService.For<IInvalidReturnType>("https://api.github.com"));
        AssertExceptionContains("is invalid. All REST Methods must return either Task<T> or ValueTask<T> or IObservable<T>", exception);
    }

    private static void AssertExceptionContains(string expectedSubstring, Exception exception)
    {
        Assert.Contains(expectedSubstring, exception.Message!, StringComparison.Ordinal);
    }
}
