namespace CloudManager.Models.Aws.SecretsManager;

public sealed record SecretInfo(
    string Name,
    string Arn,
    DateTime? LastChanged,
    bool RotationEnabled);
