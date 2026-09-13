namespace CloudManager.Services.Aws;

using Amazon.RDS;
using Amazon.RDS.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Rds;

public sealed class RdsService
{
    private readonly AwsClientFactory factory;

    public RdsService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists all RDS instances (paged)
    public async ValueTask<List<RdsInstanceInfo>> ListInstancesAsync(string? engine, CancellationToken cancellationToken = default)
    {
        using var rds = factory.CreateRdsClient();
        var result = new List<RdsInstanceInfo>();
        string? marker = null;

        do
        {
            var response = await rds.DescribeDBInstancesAsync(
                new DescribeDBInstancesRequest { Marker = marker },
                cancellationToken);

            foreach (var db in response.DBInstances ?? [])
            {
                if (!String.IsNullOrWhiteSpace(engine) &&
                    !db.Engine.StartsWith(engine, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var endpoint = db.Endpoint is not null ? $"{db.Endpoint.Address}:{db.Endpoint.Port}" : null;
                result.Add(new RdsInstanceInfo(
                    db.DBInstanceIdentifier,
                    db.Engine,
                    db.EngineVersion,
                    db.DBInstanceStatus,
                    db.DBInstanceClass,
                    endpoint,
                    db.MultiAZ.GetValueOrDefault()));
            }

            marker = response.Marker;
        }
        while (!String.IsNullOrEmpty(marker));

        return result;
    }

    // Starts an RDS instance, polling until available when wait is set
    public async ValueTask StartInstanceAsync(string id, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var rds = factory.CreateRdsClient();
        await rds.StartDBInstanceAsync(
            new StartDBInstanceRequest { DBInstanceIdentifier = id },
            cancellationToken);

        if (!wait)
        {
            return;
        }

        await WaitForStatusAsync(rds, id, "available", timeoutSeconds, progress, cancellationToken);
    }

    // Stops an RDS instance, polling until stopped when wait is set
    public async ValueTask StopInstanceAsync(string id, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var rds = factory.CreateRdsClient();
        await rds.StopDBInstanceAsync(
            new StopDBInstanceRequest { DBInstanceIdentifier = id },
            cancellationToken);

        if (!wait)
        {
            return;
        }

        await WaitForStatusAsync(rds, id, "stopped", timeoutSeconds, progress, cancellationToken);
    }

    // Lists snapshots, limited to one instance when id is given
    public async ValueTask<List<RdsSnapshotInfo>> ListSnapshotsAsync(string? dbInstanceId, CancellationToken cancellationToken = default)
    {
        using var rds = factory.CreateRdsClient();
        var result = new List<RdsSnapshotInfo>();
        string? marker = null;

        do
        {
            var request = new DescribeDBSnapshotsRequest { Marker = marker };
            if (!String.IsNullOrWhiteSpace(dbInstanceId))
            {
                request.DBInstanceIdentifier = dbInstanceId;
            }

            var response = await rds.DescribeDBSnapshotsAsync(request, cancellationToken);

            foreach (var snap in response.DBSnapshots ?? [])
            {
                result.Add(new RdsSnapshotInfo(
                    snap.DBSnapshotIdentifier,
                    snap.DBInstanceIdentifier,
                    snap.Status,
                    snap.Engine,
                    snap.EngineVersion,
                    snap.SnapshotCreateTime,
                    snap.AllocatedStorage.GetValueOrDefault()));
            }

            marker = response.Marker;
        }
        while (!String.IsNullOrEmpty(marker));

        return result;
    }

    // Creates a snapshot, polling until available when wait is set
    public async ValueTask CreateSnapshotAsync(string dbInstanceId, string snapshotId, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var rds = factory.CreateRdsClient();
        await rds.CreateDBSnapshotAsync(
            new CreateDBSnapshotRequest
            {
                DBInstanceIdentifier = dbInstanceId,
                DBSnapshotIdentifier = snapshotId
            },
            cancellationToken);

        if (!wait)
        {
            return;
        }

        await WaitForSnapshotStatusAsync(rds, snapshotId, "available", timeoutSeconds, progress, cancellationToken);
    }

    // Restores an instance from a snapshot, polling until available when wait is set
    public async ValueTask RestoreSnapshotAsync(string snapshotId, string newDbInstanceId, string? instanceClass, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var rds = factory.CreateRdsClient();
        var request = new RestoreDBInstanceFromDBSnapshotRequest
        {
            DBSnapshotIdentifier = snapshotId,
            DBInstanceIdentifier = newDbInstanceId
        };
        if (!String.IsNullOrWhiteSpace(instanceClass))
        {
            request.DBInstanceClass = instanceClass;
        }

        await rds.RestoreDBInstanceFromDBSnapshotAsync(
            request,
            cancellationToken);

        if (!wait)
        {
            return;
        }

        await WaitForStatusAsync(rds, newDbInstanceId, "available", timeoutSeconds, progress, cancellationToken);
    }

    // Deletes a snapshot
    public async ValueTask DeleteSnapshotAsync(string snapshotId, CancellationToken cancellationToken = default)
    {
        using var rds = factory.CreateRdsClient();
        await rds.DeleteDBSnapshotAsync(
            new DeleteDBSnapshotRequest { DBSnapshotIdentifier = snapshotId },
            cancellationToken);
    }

    private static async ValueTask WaitForStatusAsync(AmazonRDSClient rds, string id, string targetStatus, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        var pollInterval = TimeSpan.FromSeconds(10);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(pollInterval, cancellationToken);

            var response = await rds.DescribeDBInstancesAsync(
                new DescribeDBInstancesRequest { DBInstanceIdentifier = id },
                cancellationToken);
            var status = response.DBInstances.FirstOrDefault()?.DBInstanceStatus ?? "unknown";
            var elapsed = (DateTime.UtcNow - (deadline - TimeSpan.FromSeconds(timeoutSeconds))).TotalSeconds;

            progress.Report(new ProgressUpdate(Math.Min(elapsed / timeoutSeconds, 0.99), $"[{status}]"));

            if (String.Equals(status, targetStatus, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        throw new TimeoutException($"RDS instance '{id}' did not reach '{targetStatus}' within {timeoutSeconds} seconds.");
    }

    private static async ValueTask WaitForSnapshotStatusAsync(AmazonRDSClient rds, string snapshotId, string targetStatus, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        var pollInterval = TimeSpan.FromSeconds(10);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(pollInterval, cancellationToken);

            var response = await rds.DescribeDBSnapshotsAsync(
                new DescribeDBSnapshotsRequest { DBSnapshotIdentifier = snapshotId },
                cancellationToken);
            var status = response.DBSnapshots.FirstOrDefault()?.Status ?? "unknown";
            var elapsed = (DateTime.UtcNow - (deadline - TimeSpan.FromSeconds(timeoutSeconds))).TotalSeconds;

            progress.Report(new ProgressUpdate(Math.Min(elapsed / timeoutSeconds, 0.99), $"[{status}]"));

            if (String.Equals(status, targetStatus, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        throw new TimeoutException($"Snapshot '{snapshotId}' did not reach '{targetStatus}' within {timeoutSeconds} seconds.");
    }

    public async ValueTask RebootForFailoverAsync(string id, CancellationToken cancellationToken = default)
    {
        using var rds = factory.CreateRdsClient();
        await rds.RebootDBInstanceAsync(
            new RebootDBInstanceRequest { DBInstanceIdentifier = id, ForceFailover = true },
            cancellationToken);
    }
}
