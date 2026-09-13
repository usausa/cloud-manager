namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class SqsPage
{
    [Inject]
    public required SqsService Service { get; set; }

    private List<SqsQueueInfo> queues = [];

    private string searchText = string.Empty;

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            queues = await Service.ListQueuesAsync(CancellationToken);
        });

    private async Task SendMessageAsync(SqsQueueInfo queue)
    {
        var parameters = new DialogParameters<SqsSendMessageDialog>
        {
            { x => x.QueueName, queue.QueueName }
        };
        var dialog = await DialogService.ShowAsync<SqsSendMessageDialog>("メッセージ送信", parameters);
        var dialogResult = await dialog.Result;
        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        var p = (SqsSendMessageParams)dialogResult.Data!;

        await RunAsync("送信中...", async (_, cancellationToken) =>
        {
            await Service.SendMessageAsync(queue.QueueUrl, p.Body, 0, null, cancellationToken);
            Snackbar.AddSuccess($"メッセージ送信完了: {queue.QueueName}");
            await LoadAsync();
        });
    }

    private Task ReceiveMessagesAsync(SqsQueueInfo queue) =>
        RunAsync("受信中...", async (_, cancellationToken) =>
        {
            var messages = await Service.ReceiveMessagesAsync(queue.QueueUrl, maxMessages: 10, cancellationToken);
            var parameters = new DialogParameters<SqsMessagesDialog>
            {
                { x => x.QueueName, queue.QueueName },
                { x => x.Messages, messages }
            };
            await DialogService.ShowAsync<SqsMessagesDialog>("受信メッセージ", parameters);
        });

    private async Task PurgeAsync(SqsQueueInfo queue)
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { x => x.Title, "パージ確認" },
            { x => x.Message, $"キュー {queue.QueueName} のメッセージをすべて削除しますか？" },
            { x => x.RequireConfirmText, queue.QueueName }
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("パージ確認", parameters);
        var dialogResult = await dialog.Result;
        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        await RunAsync("パージ中...", async (_, cancellationToken) =>
        {
            await Service.PurgeQueueAsync(queue.QueueUrl, cancellationToken);
            Snackbar.AddSuccess($"パージ完了: {queue.QueueName}");
            await LoadAsync();
        });
    }

    private bool FilterFunc(SqsQueueInfo q) =>
        String.IsNullOrWhiteSpace(searchText) || q.QueueName.Contains(searchText, StringComparison.OrdinalIgnoreCase);
}
