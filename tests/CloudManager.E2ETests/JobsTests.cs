namespace CloudManager;

using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

public sealed class JobsTests : PageTest
{
    // AWS へ接続せずに完結する画面で、追加ダイアログの検証と登録・削除を通す
    [Fact]
    public async Task AddAndDeleteJob()
    {
        // Arrange
        await using var factory = new E2EApplicationFactory();
        factory.UseKestrel(0);
        factory.StartServer();

        // Act
        await Page.GotoAsync(factory.ServerAddress + "/jobs");
        await Expect(Page.GetByText("ジョブがありません。")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "追加" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "保存" }).ClickAsync();
        await Expect(Page.GetByText("ジョブ名を入力してください。")).ToBeVisibleAsync();

        await Page.GetByLabel("ジョブ名").FillAsync("e2e-job");
        await Page.GetByLabel("EC2 インスタンス ID").FillAsync("i-0123456789abcdef0");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "保存" }).ClickAsync();

        // Assert
        await Expect(Page.GetByRole(AriaRole.Cell, new PageGetByRoleOptions { Name = "e2e-job" })).ToBeVisibleAsync();

        // Cleanup
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "削除" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "OK" }).ClickAsync();
        await Expect(Page.GetByText("ジョブがありません。")).ToBeVisibleAsync();
    }
}
