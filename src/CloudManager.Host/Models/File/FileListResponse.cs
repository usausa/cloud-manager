namespace CloudManager.Host.Models.File;

public sealed record FileListResponse(IReadOnlyList<string> Entries);
