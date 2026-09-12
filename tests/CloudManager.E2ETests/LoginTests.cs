namespace CloudManager;

using System.Text.RegularExpressions;

using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

public sealed class LoginTests : PageTest
{
    [Fact]
    public async Task LoginShowsHomePage()
    {
        // Arrange
        await using var factory = new E2EApplicationFactory();
        factory.UseKestrel(0);
        factory.StartServer();

        // Act
        await Page.GotoAsync(factory.ServerAddress + "/");
        await Expect(Page).ToHaveURLAsync(new Regex(".*login.*"));

        await Page.FillAsync("#name", "admin");
        await Page.FillAsync("#password", "admin");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "ログイン" }).ClickAsync();

        // Assert
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "CloudManager" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task LoginWithWrongPasswordShowsError()
    {
        // Arrange
        await using var factory = new E2EApplicationFactory();
        factory.UseKestrel(0);
        factory.StartServer();

        // Act
        await Page.GotoAsync(factory.ServerAddress + "/login");
        await Page.FillAsync("#name", "admin");
        await Page.FillAsync("#password", "wrong");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "ログイン" }).ClickAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(new Regex(".*error=1.*"));
        await Expect(Page.Locator(".mud-alert")).ToBeVisibleAsync();
    }
}
