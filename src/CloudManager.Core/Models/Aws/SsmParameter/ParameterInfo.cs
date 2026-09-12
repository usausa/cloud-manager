namespace CloudManager.Models.Aws.SsmParameter;

public sealed record ParameterInfo(
    string Name,
    string Type,
    DateTime? LastModified,
    int Version);
