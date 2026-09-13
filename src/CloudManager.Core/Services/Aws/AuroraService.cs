namespace CloudManager.Services.Aws;

using Amazon.RDS.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Rds;

// Aurora cluster operations
public sealed class AuroraService
{
    private readonly AwsClientFactory factory;

    public AuroraService(AwsClientFactory factory) => this.factory = factory;

    public async ValueTask<List<AuroraClusterInfo>> ListClustersAsync(CancellationToken cancellationToken = default)
    {
        using var rds = factory.CreateRdsClient();
        var result = new List<AuroraClusterInfo>();
        string? marker = null;
        do
        {
            var response = await rds.DescribeDBClustersAsync(
                new DescribeDBClustersRequest { Marker = marker },
                cancellationToken);
            foreach (var c in response.DBClusters ?? [])
            {
                var members = (c.DBClusterMembers ?? [])
                    .Select(m => new AuroraClusterMember(
                        m.DBInstanceIdentifier ?? string.Empty,
                        m.IsClusterWriter.GetValueOrDefault() ? "Writer" : "Reader",
                        m.DBClusterParameterGroupStatus ?? string.Empty))
                    .ToList();
                result.Add(new AuroraClusterInfo(
                    c.DBClusterIdentifier ?? string.Empty,
                    c.Engine ?? string.Empty,
                    c.EngineVersion ?? string.Empty,
                    c.Status ?? string.Empty,
                    c.Endpoint,
                    c.ReaderEndpoint,
                    members));
            }
            marker = response.Marker;
        }
        while (!String.IsNullOrEmpty(marker));
        return result;
    }

    public async ValueTask FailoverClusterAsync(string clusterId, string? targetInstance, CancellationToken cancellationToken = default)
    {
        using var rds = factory.CreateRdsClient();
        await rds.FailoverDBClusterAsync(
            new FailoverDBClusterRequest
            {
                DBClusterIdentifier = clusterId,
                TargetDBInstanceIdentifier = targetInstance
            },
            cancellationToken);
    }
}
