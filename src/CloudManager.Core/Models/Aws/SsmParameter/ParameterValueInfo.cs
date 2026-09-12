namespace CloudManager.Models.Aws.SsmParameter;

public sealed record ParameterValueInfo(
    string Name,
    string Type,
    string Value);
