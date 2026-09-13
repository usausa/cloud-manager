namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class SnsPage
{
    [Inject]
    public required SnsService Service { get; set; }

    private List<SnsTopicInfo> topics = [];

    private List<SnsSubscriptionInfo> subscriptions = [];

    private SnsTopicInfo? selectedTopic;

    private bool isSubsLoading;

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync()
    {
        selectedTopic = null;
        subscriptions = [];
        return LoadAsync(async () =>
        {
            topics = await Service.ListTopicsAsync(CancellationToken);
        });
    }

    private Task OnTopicSelectedAsync(SnsTopicInfo? topic)
    {
        selectedTopic = topic;
        subscriptions = [];
        if (topic is null)
        {
            return Task.CompletedTask;
        }

        return LoadAsync(async () =>
        {
            subscriptions = await Service.ListSubscriptionsAsync(topic.TopicArn, CancellationToken);
        }, x => isSubsLoading = x);
    }

    private async Task PublishAsync(SnsTopicInfo topic)
    {
        var dialogParams = new DialogParameters<SnsPublishDialog>
        {
            { x => x.TopicArn, topic.TopicArn }
        };
        var dialog = await DialogService.ShowAsync<SnsPublishDialog>("メッセージ送信", dialogParams, Styles.MediumDialog);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }
        var p = (SnsPublishParams)result.Data!;

        await RunAsync("送信中...", async (_, cancellationToken) =>
        {
            var messageId = await Service.PublishAsync(topic.TopicArn, p.Subject, p.Message, cancellationToken);
            Snackbar.AddSuccess($"メッセージを送信しました。(MessageId: {messageId})");
        });
    }

    private static string ShortArn(string arn)
    {
        var lastColon = arn.LastIndexOf(':');
        return lastColon >= 0 ? arn[(lastColon + 1)..] : arn;
    }
}
