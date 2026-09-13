namespace CloudManager.Services.Aws;

using Amazon.RDS.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Rds;

// RDS parameter group operations
public sealed class RdsParamGroupService
{
    private readonly AwsClientFactory factory;

    public RdsParamGroupService(AwsClientFactory factory) => this.factory = factory;

    public async ValueTask<List<RdsParamGroupInfo>> ListParameterGroupsAsync(CancellationToken cancellationToken = default)
    {
        using var rds = factory.CreateRdsClient();
        var result = new List<RdsParamGroupInfo>();
        string? marker = null;
        do
        {
            var response = await rds.DescribeDBParameterGroupsAsync(
                new DescribeDBParameterGroupsRequest { Marker = marker },
                cancellationToken);
            foreach (var g in response.DBParameterGroups ?? [])
            {
                result.Add(new RdsParamGroupInfo(
                    g.DBParameterGroupName ?? string.Empty,
                    g.DBParameterGroupFamily ?? string.Empty,
                    g.Description ?? string.Empty));
            }
            marker = response.Marker;
        }
        while (!String.IsNullOrEmpty(marker));
        return result;
    }

    public async ValueTask<List<RdsParameterInfo>> ListParametersAsync(string groupName, CancellationToken cancellationToken = default)
    {
        using var rds = factory.CreateRdsClient();
        var result = new List<RdsParameterInfo>();
        string? marker = null;
        do
        {
            var response = await rds.DescribeDBParametersAsync(
                new DescribeDBParametersRequest { DBParameterGroupName = groupName, Marker = marker },
                cancellationToken);
            foreach (var p in response.Parameters ?? [])
            {
                result.Add(new RdsParameterInfo(
                    p.ParameterName ?? string.Empty,
                    p.ParameterValue,
                    null,
                    p.ApplyType ?? string.Empty,
                    p.IsModifiable.GetValueOrDefault(),
                    p.Source));
            }
            marker = response.Marker;
        }
        while (!String.IsNullOrEmpty(marker));
        return result;
    }
}
