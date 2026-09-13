namespace CloudManager.Models.Aws.S3;

public sealed record S3ObjectHead(long ContentLength, string ContentType);
