namespace CloudManager.Models.Aws.Cognito;

public sealed record CognitoUserInfo(
    string Username,
    string Status,
    bool Enabled,
    DateTime? Created,
    string? Email);
