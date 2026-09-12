namespace CloudManager.Models.Aws.S3;

public sealed record S3ObjectInfo(string Key, long Size, DateTime LastModified, string StorageClass);
