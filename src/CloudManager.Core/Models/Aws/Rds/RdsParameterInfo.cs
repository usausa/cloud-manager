namespace CloudManager.Models.Aws.Rds;

public sealed record RdsParameterInfo(string Name, string? Value, string? DefaultValue, string ApplyType, bool IsModifiable, string? Source);
