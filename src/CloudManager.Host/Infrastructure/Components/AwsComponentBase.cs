namespace CloudManager.Host.Infrastructure.Components;

using Amazon.Runtime;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// AWS を操作するページの共通基底。読み込み/実行中の状態とエラー表示を一箇所にまとめる
public abstract class AwsComponentBase : AppComponentBase
{
    private CancellationTokenSource? cancellation;

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    protected bool IsLoading { get; private set; }

    protected bool IsRunning { get; private set; }

    protected double ProgressRatio { get; private set; }

    protected string? ProgressMessage { get; private set; }

    protected string? ErrorMessage { get; set; }

    // 回線が切れたら実行中の操作を打ち切る
    protected CancellationToken CancellationToken => (cancellation ??= new CancellationTokenSource()).Token;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (cancellation is not null))
        {
            cancellation.Cancel();
            cancellation.Dispose();
            cancellation = null;
        }

        base.Dispose(disposing);
    }

    // 一覧取得。失敗はバナーに表示する
    protected async Task LoadAsync(Func<Task> load)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await load();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // 操作の実行。進捗オーバーレイを表示し、失敗はバナーとスナックバーに表示する
    protected async Task RunAsync(Func<IProgress<ProgressUpdate>, CancellationToken, Task> operation, Func<Task>? reload = null)
    {
        IsRunning = true;
        ProgressRatio = 0;
        ProgressMessage = "実行中...";
        ErrorMessage = null;

        var progress = new Progress<ProgressUpdate>(x =>
        {
            ProgressRatio = x.Ratio;
            ProgressMessage = x.Message;
            _ = InvokeAsync(StateHasChanged);
        });

        try
        {
            await operation(progress, CancellationToken);
            if (reload is not null)
            {
                await reload();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
            Snackbar.AddError(ErrorMessage);
        }
        finally
        {
            IsRunning = false;
            ProgressRatio = 0;
            ProgressMessage = null;
        }
    }

    protected static string FormatError(Exception ex) =>
        ex is AmazonServiceException aws ? $"[{aws.ErrorCode}] {aws.Message}" : ex.Message;
}
