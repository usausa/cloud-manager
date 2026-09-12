namespace CloudManager.Models;

// 進捗更新情報。
public sealed record ProgressUpdate(double Ratio, string? Message);
