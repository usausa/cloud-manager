namespace CloudManager.Services.Aws;

using Amazon.DynamoDBv2.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.DynamoDb;

public sealed class DynamoDbService
{
    private readonly AwsClientFactory factory;

    public DynamoDbService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists DynamoDB tables (paged)
    public async ValueTask<List<DynamoDbTableInfo>> ListTablesAsync(CancellationToken cancellationToken = default)
    {
        using var dynamoDb = factory.CreateDynamoDbClient();
        var result = new List<DynamoDbTableInfo>();
        string? lastEvaluatedTableName = null;

        do
        {
            var listResponse = await dynamoDb.ListTablesAsync(
                new ListTablesRequest
                {
                    ExclusiveStartTableName = lastEvaluatedTableName
                },
                cancellationToken);

            foreach (var tableName in listResponse.TableNames)
            {
                var descResponse = await dynamoDb.DescribeTableAsync(
                    new DescribeTableRequest
                    {
                        TableName = tableName
                    },
                    cancellationToken);

                var table = descResponse.Table;
                result.Add(new DynamoDbTableInfo(
                    table.TableName,
                    table.TableStatus.Value,
                    table.ItemCount.GetValueOrDefault(),
                    table.TableSizeBytes.GetValueOrDefault(),
                    table.BillingModeSummary?.BillingMode?.Value ?? "PROVISIONED",
                    table.GlobalSecondaryIndexes?.Count ?? 0));
            }

            lastEvaluatedTableName = listResponse.LastEvaluatedTableName;
        }
        while (!String.IsNullOrEmpty(lastEvaluatedTableName));

        return result;
    }

    // Scans table items (up to limit)
    public async ValueTask<List<DynamoDbItemInfo>> ScanAsync(string tableName, int limit = 100, CancellationToken cancellationToken = default)
    {
        using var dynamoDb = factory.CreateDynamoDbClient();
        var response = await dynamoDb.ScanAsync(
            new ScanRequest
            {
                TableName = tableName,
                Limit = limit
            },
            cancellationToken);
#pragma warning disable IDE0028
        return (response.Items ?? [])
            .Select(item => new DynamoDbItemInfo(item.ToDictionary(kv => kv.Key, kv => AttributeValueToString(kv.Value))))
            .ToList();
#pragma warning restore IDE0028
    }

    // Gets the TTL setting
    public async ValueTask<DynamoDbTtlInfo> GetTtlAsync(string tableName, CancellationToken cancellationToken = default)
    {
        using var dynamoDb = factory.CreateDynamoDbClient();
        var response = await dynamoDb.DescribeTimeToLiveAsync(new DescribeTimeToLiveRequest { TableName = tableName }, cancellationToken);
        var enabled = response.TimeToLiveDescription?.TimeToLiveStatus?.Value == "ENABLED";
        return new DynamoDbTtlInfo(tableName, enabled, response.TimeToLiveDescription?.AttributeName);
    }

    // Enables or disables TTL
    public async ValueTask UpdateTtlAsync(string tableName, bool enable, string attributeName, CancellationToken cancellationToken = default)
    {
        using var dynamoDb = factory.CreateDynamoDbClient();
        await dynamoDb.UpdateTimeToLiveAsync(
            new UpdateTimeToLiveRequest
            {
                TableName = tableName,
                TimeToLiveSpecification = new TimeToLiveSpecification
                {
                    Enabled = enable,
                    AttributeName = attributeName
                }
            },
            cancellationToken);
    }

    // Gets the point-in-time recovery setting
    public async ValueTask<DynamoDbPitrInfo> GetPitrAsync(string tableName, CancellationToken cancellationToken = default)
    {
        using var dynamoDb = factory.CreateDynamoDbClient();
        var response = await dynamoDb.DescribeContinuousBackupsAsync(new DescribeContinuousBackupsRequest { TableName = tableName }, cancellationToken);
        var pitr = response.ContinuousBackupsDescription?.PointInTimeRecoveryDescription;
        var enabled = pitr?.PointInTimeRecoveryStatus?.Value == "ENABLED";
        return new DynamoDbPitrInfo(tableName, enabled, pitr?.EarliestRestorableDateTime, pitr?.LatestRestorableDateTime);
    }

    // Enables or disables point-in-time recovery
    public async ValueTask UpdatePitrAsync(string tableName, bool enable, CancellationToken cancellationToken = default)
    {
        using var dynamoDb = factory.CreateDynamoDbClient();
        await dynamoDb.UpdateContinuousBackupsAsync(
            new UpdateContinuousBackupsRequest
            {
                TableName = tableName,
                PointInTimeRecoverySpecification = new PointInTimeRecoverySpecification { PointInTimeRecoveryEnabled = enable }
            },
            cancellationToken);
    }

    private static string AttributeValueToString(AttributeValue v)
    {
        if (v.S is not null)
        {
            return v.S;
        }

        if (v.N is not null)
        {
            return v.N;
        }

        if (v.BOOL.GetValueOrDefault())
        {
            return "true";
        }

        if (v.NULL.GetValueOrDefault())
        {
            return "(null)";
        }

        if (v.SS?.Count > 0)
        {
            return $"[{String.Join(", ", v.SS)}]";
        }

        if (v.NS?.Count > 0)
        {
            return $"[{String.Join(", ", v.NS)}]";
        }

        if (v.L?.Count > 0)
        {
            return $"(list:{v.L.Count})";
        }

        if (v.M?.Count > 0)
        {
            return $"(map:{v.M.Count})";
        }

        if (v.B is not null)
        {
            return "(binary)";
        }

        return string.Empty;
    }
}
