namespace CloudManager.Services.Aws;

using Amazon.Route53;
using Amazon.Route53.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Route53;

public sealed class Route53Service
{
    private readonly AwsClientFactory factory;

    public Route53Service(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    public async ValueTask<List<HostedZoneInfo>> ListHostedZonesAsync(CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateRoute53Client();
        var results = new List<HostedZoneInfo>();
        string? marker = null;
        do
        {
            var request = new ListHostedZonesRequest { Marker = marker };
            var response = await client.ListHostedZonesAsync(request, cancellationToken);
            foreach (var z in response.HostedZones ?? [])
            {
                results.Add(new HostedZoneInfo(
                    z.Id ?? string.Empty,
                    z.Name ?? string.Empty,
                    (int)z.ResourceRecordSetCount.GetValueOrDefault(),
                    z.Config is not null && z.Config.PrivateZone.GetValueOrDefault()));
            }

            marker = response.IsTruncated.GetValueOrDefault() ? response.NextMarker : null;
        }
        while (!String.IsNullOrEmpty(marker));
        return results;
    }

    public async ValueTask<List<RecordSetInfo>> ListRecordSetsAsync(string hostedZoneId, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateRoute53Client();
        var results = new List<RecordSetInfo>();
        string? startRecordName = null;
        string? startRecordType = null;
        bool isTruncated;
        do
        {
            var request = new ListResourceRecordSetsRequest { HostedZoneId = hostedZoneId };
            if (startRecordName is not null)
            {
                request.StartRecordName = startRecordName;
                request.StartRecordType = startRecordType;
            }
            var response = await client.ListResourceRecordSetsAsync(request, cancellationToken);
            foreach (var r in response.ResourceRecordSets ?? [])
            {
                var values = r.ResourceRecords.Select(rr => rr.Value ?? string.Empty).ToList();
                if (r.AliasTarget is not null)
                {
                    values = [$"ALIAS {r.AliasTarget.DNSName}"];
                }
                results.Add(new RecordSetInfo(
                    r.Name ?? string.Empty,
                    r.Type?.Value ?? string.Empty,
                    r.TTL,
                    values));
            }
            isTruncated = response.IsTruncated.GetValueOrDefault();
            startRecordName = response.NextRecordName;
            startRecordType = response.NextRecordType?.Value;
        }
        while (isTruncated);
        return results;
    }

    public async ValueTask UpsertRecordAsync(
        string hostedZoneId,
        string name,
        string type,
        int ttl,
        IEnumerable<string> values,
        CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateRoute53Client();
#pragma warning disable IDE0028
        await client.ChangeResourceRecordSetsAsync(
            new ChangeResourceRecordSetsRequest
            {
                HostedZoneId = hostedZoneId,
                ChangeBatch = new ChangeBatch
                {
                    Changes =
                    [
                        new Change
                        {
                            Action = ChangeAction.UPSERT,
                            ResourceRecordSet = new ResourceRecordSet
                            {
                                Name = name,
                                Type = type,
                                TTL = ttl,
                                ResourceRecords = values.Select(v => new ResourceRecord { Value = v }).ToList()
                            }
                        }
                    ]
                }
            },
            cancellationToken);
#pragma warning restore IDE0028
    }

    public async ValueTask DeleteRecordAsync(
        string hostedZoneId,
        string name,
        string type,
        int ttl,
        IEnumerable<string> values,
        CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateRoute53Client();
#pragma warning disable IDE0028
        await client.ChangeResourceRecordSetsAsync(
            new ChangeResourceRecordSetsRequest
            {
                HostedZoneId = hostedZoneId,
                ChangeBatch = new ChangeBatch
                {
                    Changes =
                    [
                        new Change
                        {
                            Action = ChangeAction.DELETE,
                            ResourceRecordSet = new ResourceRecordSet
                            {
                                Name = name,
                                Type = type,
                                TTL = ttl,
                                ResourceRecords = values.Select(v => new ResourceRecord { Value = v }).ToList()
                            }
                        }
                    ]
                }
            },
            cancellationToken);
#pragma warning restore IDE0028
    }
}
