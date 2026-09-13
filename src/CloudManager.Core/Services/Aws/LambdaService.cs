namespace CloudManager.Services.Aws;

using Amazon.Lambda.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Lambda;

public sealed class LambdaService
{
    private readonly AwsClientFactory factory;

    public LambdaService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists Lambda functions (paged)
    public async ValueTask<List<LambdaFunctionInfo>> ListFunctionsAsync(CancellationToken cancellationToken = default)
    {
        using var lambda = factory.CreateLambdaClient();
        var result = new List<LambdaFunctionInfo>();
        string? marker = null;

        do
        {
            var response = await lambda.ListFunctionsAsync(
                new ListFunctionsRequest { Marker = marker },
                cancellationToken);

            foreach (var fn in response.Functions ?? [])
            {
                DateTime? lastModified = DateTime.TryParse(fn.LastModified, CultureInfo.InvariantCulture, out var dt) ? dt : null;
                result.Add(new LambdaFunctionInfo(
                    fn.FunctionName,
                    fn.Runtime?.Value ?? "-",
                    fn.Handler ?? "-",
                    fn.CodeSize.GetValueOrDefault(),
                    fn.State?.Value ?? "-",
                    lastModified));
            }

            marker = response.NextMarker;
        }
        while (!String.IsNullOrEmpty(marker));

        return result;
    }

    // Invokes a Lambda function with a JSON payload
    public async ValueTask<LambdaInvokeResult> InvokeAsync(string functionName, string? payload, string invocationType, CancellationToken cancellationToken = default)
    {
        using var lambda = factory.CreateLambdaClient();
        var request = new InvokeRequest
        {
            FunctionName = functionName,
            InvocationType = invocationType,
            Payload = payload ?? string.Empty,
            LogType = "Tail"
        };

        var response = await lambda.InvokeAsync(
            request,
            cancellationToken);

        string responsePayload;
        using (var reader = new StreamReader(response.Payload))
        {
            responsePayload = await reader.ReadToEndAsync(cancellationToken);
        }

        string? logResult = null;
        if (!String.IsNullOrEmpty(response.LogResult))
        {
            logResult = Encoding.UTF8.GetString(Convert.FromBase64String(response.LogResult));
        }

        return new LambdaInvokeResult(response.StatusCode.GetValueOrDefault(), response.FunctionError, responsePayload, logResult);
    }

    // Gets environment variables
    public async ValueTask<List<LambdaEnvVarInfo>> GetEnvironmentVariablesAsync(string functionName, CancellationToken cancellationToken = default)
    {
        using var lambda = factory.CreateLambdaClient();
        var response = await lambda.GetFunctionConfigurationAsync(new GetFunctionConfigurationRequest { FunctionName = functionName }, cancellationToken);
        return response.Environment?.Variables?
            .Select(kv => new LambdaEnvVarInfo(kv.Key, kv.Value))
            .OrderBy(e => e.Key)
            .ToList() ?? [];
    }

    // Updates environment variables, merging with existing ones
    public async ValueTask UpdateEnvironmentVariablesAsync(string functionName, Dictionary<string, string> variables, CancellationToken cancellationToken = default)
    {
        using var lambda = factory.CreateLambdaClient();
        await lambda.UpdateFunctionConfigurationAsync(
            new UpdateFunctionConfigurationRequest
            {
                FunctionName = functionName,
                Environment = new Environment { Variables = variables }
            },
            cancellationToken);
    }

    // Lists aliases
    public async ValueTask<List<LambdaAliasInfo>> ListAliasesAsync(string functionName, CancellationToken cancellationToken = default)
    {
        using var lambda = factory.CreateLambdaClient();
        var result = new List<LambdaAliasInfo>();
        string? marker = null;
        do
        {
            var response = await lambda.ListAliasesAsync(
                new ListAliasesRequest { FunctionName = functionName, Marker = marker },
                cancellationToken);
            foreach (var a in response.Aliases ?? [])
            {
                var routing = a.RoutingConfig?.AdditionalVersionWeights?.FirstOrDefault();
                result.Add(new LambdaAliasInfo(
                    a.Name,
                    a.FunctionVersion,
                    a.Description,
                    routing?.Key,
                    routing?.Value));
            }

            marker = response.NextMarker;
        }
        while (!String.IsNullOrEmpty(marker));
        return result;
    }

    // Creates an alias
    public async ValueTask CreateAliasAsync(string functionName, string aliasName, string functionVersion, string? description, CancellationToken cancellationToken = default)
    {
        using var lambda = factory.CreateLambdaClient();
        await lambda.CreateAliasAsync(
            new CreateAliasRequest
            {
                FunctionName = functionName,
                Name = aliasName,
                FunctionVersion = functionVersion,
                Description = description ?? string.Empty
            },
            cancellationToken);
    }

    // Deletes an alias
    public async ValueTask DeleteAliasAsync(string functionName, string aliasName, CancellationToken cancellationToken = default)
    {
        using var lambda = factory.CreateLambdaClient();
        await lambda.DeleteAliasAsync(
            new DeleteAliasRequest { FunctionName = functionName, Name = aliasName },
            cancellationToken);
    }

    // Gets the reserved concurrency
    public async ValueTask<LambdaConcurrencyInfo> GetConcurrencyAsync(string functionName, CancellationToken cancellationToken = default)
    {
        using var lambda = factory.CreateLambdaClient();
        int? reserved = null;
        try
        {
            var r = await lambda.GetFunctionConcurrencyAsync(new GetFunctionConcurrencyRequest { FunctionName = functionName }, cancellationToken);
            reserved = r.ReservedConcurrentExecutions;
        }
        catch (ResourceNotFoundException)
        {
        }

        return new LambdaConcurrencyInfo(functionName, reserved, null);
    }

    // Sets the reserved concurrency (null removes it)
    public async ValueTask SetReservedConcurrencyAsync(string functionName, int? value, CancellationToken cancellationToken = default)
    {
        using var lambda = factory.CreateLambdaClient();
        if (value is null)
        {
            await lambda.DeleteFunctionConcurrencyAsync(
                new DeleteFunctionConcurrencyRequest { FunctionName = functionName },
                cancellationToken);
        }
        else
        {
            await lambda.PutFunctionConcurrencyAsync(
                new PutFunctionConcurrencyRequest
                {
                    FunctionName = functionName,
                    ReservedConcurrentExecutions = value.Value
                },
                cancellationToken);
        }
    }

    // Gets the dead-letter queue setting
    public async ValueTask<LambdaDlqInfo> GetDlqAsync(string functionName, CancellationToken cancellationToken = default)
    {
        using var lambda = factory.CreateLambdaClient();
        var response = await lambda.GetFunctionConfigurationAsync(new GetFunctionConfigurationRequest { FunctionName = functionName }, cancellationToken);
        return new LambdaDlqInfo(functionName, response.DeadLetterConfig?.TargetArn);
    }
}
