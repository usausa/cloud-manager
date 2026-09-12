namespace CloudManager.Components.Layout;

using Bunit;

using CloudManager.Host.Components.Layout;

public sealed class NavMenuTests : MudBlazorTestBase
{
    [Fact]
    public void RenderShowsNavigationLinks()
    {
        // Arrange & Act
        var cut = Render<NavMenu>();

        // Assert
        var links = cut.FindAll("a");
        Assert.Equal(3, links.Count);
    }
}
