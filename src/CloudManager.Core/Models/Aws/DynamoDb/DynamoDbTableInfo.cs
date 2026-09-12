namespace CloudManager.Models.Aws.DynamoDb;

public sealed record DynamoDbTableInfo(
    string TableName,
    string Status,
    long ItemCount,
    long TableSizeBytes,
    string BillingMode,
    int GsiCount);
