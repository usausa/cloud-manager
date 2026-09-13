namespace CloudManager.Models.Aws.Cognito;

public sealed record UserPoolInfo(
    string Id,
    string Name,
    DateTime? CreationDate);
