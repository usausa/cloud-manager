namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Aws;
using CloudManager.Host.Infrastructure.Components;
using CloudManager.Host.Infrastructure.Jobs;
using CloudManager.Host.Mappers;
using CloudManager.Host.Models.Forms;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class JobsPage
{
    private static readonly DialogOptions EditDialogOptions = new() { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseOnEscapeKey = true };

    private List<JobRow> rows = [];

    [Inject]
    public required AwsSession Session { get; set; }

    [Inject]
    public required JobService JobService { get; set; }

    [Inject]
    public required JobManager Manager { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            var jobs = await JobService.QueryAllAsync(CancellationToken);
            rows = jobs.Select(x => new JobRow(x, Manager.GetNextExecutionTime(x))).ToList();
        });

    private async Task AddAsync()
    {
        // 新規は現在のセッションのプロファイル/リージョンを初期値にする
        var form = await ShowEditDialog("ジョブ追加", new JobForm
        {
            ProfileName = Session.ProfileName,
            RegionName = Session.Region?.SystemName ?? string.Empty
        });
        if (form is null)
        {
            return;
        }

        await RunAsync("追加中...", async (_, cancellationToken) =>
        {
            await Manager.AddAsync(JobMapper.ToDefinition(form), cancellationToken);
            Snackbar.AddSuccess("ジョブを追加しました。");
        }, LoadAsync);
    }

    private async Task EditAsync(JobDefinition job)
    {
        var form = await ShowEditDialog("ジョブ編集", JobMapper.ToForm(job));
        if (form is null)
        {
            return;
        }

        await RunAsync("更新中...", async (_, cancellationToken) =>
        {
            if (await Manager.UpdateAsync(JobMapper.ToDefinition(form), cancellationToken))
            {
                Snackbar.AddSuccess("ジョブを更新しました。");
            }
            else
            {
                Snackbar.AddError("対象が存在しません。");
            }
        }, LoadAsync);
    }

    private async Task DeleteAsync(JobDefinition job)
    {
        if (!await DialogService.ShowConfirm("ジョブ削除", $"ジョブ「{job.Name}」を削除しますか？"))
        {
            return;
        }

        await RunAsync("削除中...", async (_, cancellationToken) =>
        {
            if (await Manager.DeleteAsync(job.Id, cancellationToken))
            {
                Snackbar.AddSuccess("ジョブを削除しました。");
            }
            else
            {
                Snackbar.AddError("対象が存在しません。");
            }
        }, LoadAsync);
    }

    private async Task RunNowAsync(JobDefinition job)
    {
        if (!await DialogService.ShowConfirm("即時実行", $"ジョブ「{job.Name}」を今すぐ実行しますか？"))
        {
            return;
        }

        await RunAsync($"実行中: {job.Name}", async (_, cancellationToken) =>
        {
            var status = await Manager.ExecuteNowAsync(job, cancellationToken);
            if (status == JobExecutionStatus.Success)
            {
                Snackbar.AddSuccess($"ジョブを実行しました: {job.Name}");
            }
            else
            {
                Snackbar.AddWarning($"ジョブが失敗しました: {job.Name} (実行履歴を確認してください)");
            }
        }, LoadAsync);
    }

    private async Task<JobForm?> ShowEditDialog(string title, JobForm form)
    {
        var reference = await DialogService.ShowAsync<JobEditDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(JobEditDialog.Title), title },
                { nameof(JobEditDialog.Form), form }
            },
            EditDialogOptions);
        var result = await reference.Result;
        return (result is { Canceled: false }) ? (JobForm)result.Data! : null;
    }

    private sealed record JobRow(JobDefinition Job, DateTimeOffset? NextExecution);
}
