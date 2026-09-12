namespace CloudManager.Models.Aws.DynamoDb;

public sealed record DynamoDbPitrInfo(string TableName, bool Enabled, DateTime? EarliestRestoreDate, DateTime? LatestRestoreDate);
