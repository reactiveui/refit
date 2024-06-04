using Xunit;

namespace Refit.Tests;

public class RefitSettingsTests
{
    [Fact]
    public void Can_CreateRefitSettings_WithoutException()
    {
        var contentSerializer = new NewtonsoftJsonContentSerializer();
        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        var urlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter();
        var formUrlEncodedParameterFormatter = new DefaultFormUrlEncodedParameterFormatter();

        var exception = Record.Exception(() => new RefitSettings());
        Assert.Null(exception);

        exception = Record.Exception(() => new RefitSettings(contentSerializer));
        Assert.Null(exception);

        exception = Record.Exception(() => new RefitSettings(contentSerializer, urlParameterFormatter));
        Assert.Null(exception);

        exception = Record.Exception(() => new RefitSettings(contentSerializer, urlParameterFormatter, formUrlEncodedParameterFormatter));
        Assert.Null(exception);

        exception = Record.Exception(() => new RefitSettings(contentSerializer, urlParameterFormatter, formUrlEncodedParameterFormatter, urlParameterKeyFormatter));
        Assert.Null(exception);
    }
}
