namespace CloudManager.Services.Aws;

using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Ec2;

public sealed class Ec2Service
{
    private readonly AwsClientFactory factory;

    public Ec2Service(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists all EC2 instances (paged)
    public async ValueTask<List<Ec2InstanceInfo>> ListInstancesAsync(string? state, string? tag, CancellationToken cancellationToken = default)
    {
        using var ec2 = factory.CreateEc2Client();
        var filters = new List<Filter>();
        if (!String.IsNullOrWhiteSpace(state))
        {
            filters.Add(new Filter("instance-state-name", [state]));
        }

        if (!String.IsNullOrWhiteSpace(tag))
        {
            var parts = tag.Split('=', 2);
            if (parts.Length == 2)
            {
                filters.Add(new Filter($"tag:{parts[0]}", [parts[1]]));
            }
        }

        var result = new List<Ec2InstanceInfo>();
        string? nextToken = null;

        do
        {
            var response = await ec2.DescribeInstancesAsync(
                new DescribeInstancesRequest
                {
                    Filters = filters.Count > 0 ? filters : null,
                    NextToken = nextToken
                },
                cancellationToken);

            foreach (var reservation in response.Reservations ?? [])
            {
                foreach (var instance in reservation.Instances)
                {
                    var name = instance.Tags.FirstOrDefault(t => t.Key == "Name")?.Value ?? string.Empty;
                    result.Add(new Ec2InstanceInfo(
                        instance.InstanceId,
                        name,
                        instance.State.Name.Value,
                        instance.InstanceType.Value,
                        instance.PublicIpAddress,
                        instance.PrivateIpAddress,
                        instance.LaunchTime.GetValueOrDefault()));
                }
            }

            nextToken = response.NextToken;
        }
        while (!String.IsNullOrEmpty(nextToken));

        return result;
    }

    // Starts instances, polling until running when wait is set
    public async ValueTask StartInstancesAsync(string[] ids, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var ec2 = factory.CreateEc2Client();
#pragma warning disable IDE0028
        await ec2.StartInstancesAsync(
            new StartInstancesRequest { InstanceIds = ids.ToList() },
            cancellationToken);
#pragma warning restore IDE0028

        if (!wait)
        {
            return;
        }

        await WaitForStateAsync(ec2, ids, "running", timeoutSeconds, progress, cancellationToken);
    }

    // Stops instances, polling until stopped when wait is set
    public async ValueTask StopInstancesAsync(string[] ids, bool force, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var ec2 = factory.CreateEc2Client();
#pragma warning disable IDE0028
        await ec2.StopInstancesAsync(
            new StopInstancesRequest { InstanceIds = ids.ToList(), Force = force },
            cancellationToken);
#pragma warning restore IDE0028

        if (!wait)
        {
            return;
        }

        await WaitForStateAsync(ec2, ids, "stopped", timeoutSeconds, progress, cancellationToken);
    }

    // Reboots instances
    public async ValueTask RebootInstancesAsync(string[] ids, CancellationToken cancellationToken = default)
    {
        using var ec2 = factory.CreateEc2Client();
#pragma warning disable IDE0028
        await ec2.RebootInstancesAsync(
            new RebootInstancesRequest { InstanceIds = ids.ToList() },
            cancellationToken);
#pragma warning restore IDE0028
    }

    // Terminates instances, polling until terminated when wait is set
    public async ValueTask TerminateInstancesAsync(string[] ids, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var ec2 = factory.CreateEc2Client();
#pragma warning disable IDE0028
        await ec2.TerminateInstancesAsync(
            new TerminateInstancesRequest { InstanceIds = ids.ToList() },
            cancellationToken);
#pragma warning restore IDE0028

        if (!wait)
        {
            return;
        }

        await WaitForStateAsync(ec2, ids, "terminated", timeoutSeconds, progress, cancellationToken);
    }

    // Runs an SSM command (AWS-RunShellScript) and waits for the output
    public async ValueTask<SsmRunResult> SsmRunAsync(string instanceId, string command, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var ssm = factory.CreateSsmClient();
        var sendResponse = await ssm.SendCommandAsync(
            new SendCommandRequest
            {
                InstanceIds = [instanceId],
                DocumentName = "AWS-RunShellScript",
                Parameters = new Dictionary<string, List<string>>
                {
                    ["commands"] = [command]
                },
                TimeoutSeconds = timeoutSeconds
            },
            cancellationToken);

        var commandId = sendResponse.Command.CommandId;
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        var pollInterval = TimeSpan.FromSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(pollInterval, cancellationToken);

            var invocation = await ssm.GetCommandInvocationAsync(
                new GetCommandInvocationRequest
                {
                    CommandId = commandId,
                    InstanceId = instanceId
                },
                cancellationToken);

            var elapsed = (DateTime.UtcNow - (deadline - TimeSpan.FromSeconds(timeoutSeconds))).TotalSeconds;
            progress.Report(new ProgressUpdate(Math.Min(elapsed / timeoutSeconds, 0.99), $"[{invocation.Status.Value}]"));

            if (invocation.Status == CommandInvocationStatus.Success ||
                invocation.Status == CommandInvocationStatus.Failed ||
                invocation.Status == CommandInvocationStatus.Cancelled ||
                invocation.Status == CommandInvocationStatus.TimedOut)
            {
                return new SsmRunResult(
                    invocation.Status.Value,
                    invocation.StandardOutputContent,
                    invocation.StandardErrorContent);
            }
        }

        throw new TimeoutException($"SSM command did not complete within {timeoutSeconds} seconds.");
    }

    private static async ValueTask WaitForStateAsync(AmazonEC2Client ec2, string[] ids, string targetState, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        var pollInterval = TimeSpan.FromSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(pollInterval, cancellationToken);

#pragma warning disable IDE0028
            var response = await ec2.DescribeInstancesAsync(
                new DescribeInstancesRequest
                {
                    InstanceIds = ids.ToList()
                },
                cancellationToken);
#pragma warning restore IDE0028

            var states = response.Reservations
                .SelectMany(r => r.Instances)
                .Select(i => i.State.Name.Value)
                .ToList();

            var done = states.All(s => s == targetState);
            var elapsed = (DateTime.UtcNow - (deadline - TimeSpan.FromSeconds(timeoutSeconds))).TotalSeconds;

            progress.Report(new ProgressUpdate(Math.Min(elapsed / timeoutSeconds, 0.99), $"[{String.Join(",", states)}]"));

            if (done)
            {
                return;
            }
        }

        throw new TimeoutException($"Instances did not reach '{targetState}' within {timeoutSeconds} seconds.");
    }
}
