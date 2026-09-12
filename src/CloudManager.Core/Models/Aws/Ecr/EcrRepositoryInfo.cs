namespace CloudManager.Models.Aws.Ecr;

public sealed record EcrRepositoryInfo(
    string Name,
    string Address,
    DateTime? CreatedAt,
    bool ImageScanOnPush,
    string EncryptionType);
