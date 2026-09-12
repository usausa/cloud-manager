namespace CloudManager.Models.Aws.DynamoDb;

public sealed record DynamoDbTtlInfo(string TableName, bool Enabled, string? AttributeName);
