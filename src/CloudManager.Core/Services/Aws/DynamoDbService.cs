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

    // DynamoDB テーブル一覧を取得する(ページング対応)。
    public async ValueTask<List<DynamoDbTableInfo>> ListTablesAsync()
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
                });

            foreach (var tableName in listResponse.TableNames)
            {
                var descResponse = await dynamoDb.DescribeTableAsync(
                    new DescribeTableRequest
                    {
                        TableName = tableName
                    });

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
        while (!string.IsNullOrEmpty(lastEvaluatedTableName));

        return result;
    }

    // テーブルのアイテムを Scan で取得する(最大 limit 件)。
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
        return (response.Items ?? [])
            .Select(item => new DynamoDbItemInfo(item.ToDictionary(kv => kv.Key, kv => AttributeValueToString(kv.Value))))
            .ToList();
    }

    // TTL 設定を取得する。
    public async ValueTask<DynamoDbTtlInfo> GetTtlAsync(string tableName)
    {
        using var dynamoDb = factory.CreateDynamoDbClient();
        var response = await dynamoDb.DescribeTimeToLiveAsync(new DescribeTimeToLiveRequest { TableName = tableName });
        var enabled = response.TimeToLiveDescription?.TimeToLiveStatus?.Value == "ENABLED";
        return new DynamoDbTtlInfo(tableName, enabled, response.TimeToLiveDescription?.AttributeName);
    }

    // TTL を有効化または無効化する。
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

    // PITR(ポイントインタイムリカバリ)設定を取得する。
    public async ValueTask<DynamoDbPitrInfo> GetPitrAsync(string tableName)
    {
        using var dynamoDb = factory.CreateDynamoDbClient();
        var response = await dynamoDb.DescribeContinuousBackupsAsync(new DescribeContinuousBackupsRequest { TableName = tableName });
        var pitr = response.ContinuousBackupsDescription?.PointInTimeRecoveryDescription;
        var enabled = pitr?.PointInTimeRecoveryStatus?.Value == "ENABLED";
        return new DynamoDbPitrInfo(tableName, enabled, pitr?.EarliestRestorableDateTime, pitr?.LatestRestorableDateTime);
    }

    // PITR を有効化または無効化する。
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
            return $"[{string.Join(", ", v.SS)}]";
        }

        if (v.NS?.Count > 0)
        {
            return $"[{string.Join(", ", v.NS)}]";
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
