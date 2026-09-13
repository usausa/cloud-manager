namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class JobHistoryPage
{
    private const int RecentLimit = 500;

    private static readonly DialogOptions ErrorDialogOptions = new() { MaxWidth = MaxWidth.Large, FullWidth = true, CloseOnEscapeKey = true };

    private List<JobExecutionLogEntity> logs = [];

    [Inject]
    public required JobLogService JobLogService { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            logs = await JobLogService.QueryRecentAsync(RecentLimit, CancellationToken);
        });

    private async Task ShowErrorAsync(JobExecutionLogEntity log)
    {
        await DialogService.ShowAsync<JobLogsDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(JobLogsDialog.Title), $"エラー詳細 - {log.JobName}" },
                { nameof(JobLogsDialog.ErrorDetail), log.ErrorDetail ?? string.Empty }
            },
            ErrorDialogOptions);
    }

    private static Color StatusColor(string status) => status switch
    {
        JobExecutionStatus.Success => Color.Success,
        JobExecutionStatus.Failure => Color.Error,
        JobExecutionStatus.Running => Color.Warning,
        _ => Color.Default
    };
}
