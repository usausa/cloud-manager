namespace CloudManager;

using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

public sealed class HomeTests : PageTest
{
    [Fact]
    public async Task RootShowsHomePage()
    {
        // Arrange
        await using var factory = new E2EApplicationFactory();
        factory.UseKestrel(0);
        factory.StartServer();

        // Act
        await Page.GotoAsync(factory.ServerAddress + "/");

        // Assert
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "CloudManager" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "ダッシュボード" })).ToBeVisibleAsync();
    }
}
