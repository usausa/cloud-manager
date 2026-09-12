namespace CloudManager.Models.Aws.SecretsManager;

public sealed record SecretValueInfo(
    string Name,
    string SecretString,
    string VersionId);
