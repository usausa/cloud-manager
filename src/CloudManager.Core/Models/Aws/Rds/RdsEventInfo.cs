namespace CloudManager.Models.Aws.Rds;

public sealed record RdsEventInfo(string SourceIdentifier, string Message, string Categories, DateTime Date);
