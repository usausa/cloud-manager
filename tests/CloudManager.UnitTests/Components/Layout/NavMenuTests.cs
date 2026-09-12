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
        var link = Assert.Single(cut.FindAll("a"));
        Assert.Equal("Home", link.TextContent.Trim());
    }
}
