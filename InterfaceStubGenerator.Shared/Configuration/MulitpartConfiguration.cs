namespace Refit.Generator.Configuration;

public class MulitpartConfiguration(string boundaryText = "----MyGreatBoundary")
{
    public string BoundaryText { get; private set; } = boundaryText;
}
