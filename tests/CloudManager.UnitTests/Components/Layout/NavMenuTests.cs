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
        var hrefs = cut.FindAll("a").Select(static x => x.GetAttribute("href")).ToList();
        Assert.Equal(28, hrefs.Count);
        Assert.Contains("ec2", hrefs);
        Assert.Contains("jobs/history", hrefs);
        Assert.Contains("settings", hrefs);
    }
}
