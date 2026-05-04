
namespace Refit.Tests;

public class RefitSettingsTests
{
    [Test]
    public void Can_CreateRefitSettings_WithoutException()
    {
        var contentSerializer = new NewtonsoftJsonContentSerializer();
        var urlParameterFormatter = new DefaultUrlParameterFormatter();
        var urlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter();
        var formUrlEncodedParameterFormatter = new DefaultFormUrlEncodedParameterFormatter();

        var exception = CaptureException(() => new RefitSettings());
        Assert.Null(exception);

        exception = CaptureException(() => new RefitSettings(contentSerializer));
        Assert.Null(exception);

        exception = CaptureException(
            () => new RefitSettings(contentSerializer, urlParameterFormatter)
        );
        Assert.Null(exception);

        exception = CaptureException(
            () =>
                new RefitSettings(
                    contentSerializer,
                    urlParameterFormatter,
                    formUrlEncodedParameterFormatter
                )
        );
        Assert.Null(exception);

        exception = CaptureException(
            () =>
                new RefitSettings(
                    contentSerializer,
                    urlParameterFormatter,
                    formUrlEncodedParameterFormatter,
                    urlParameterKeyFormatter
                )
        );
        Assert.Null(exception);
    }

    static Exception? CaptureException(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
