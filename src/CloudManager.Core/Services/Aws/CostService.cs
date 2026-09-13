namespace CloudManager.Services.Aws;

using System.Text.Json;

using Amazon.Pricing;
using Amazon.Pricing.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Cost;

public sealed class CostService
{
    private readonly AwsClientFactory factory;

    public CostService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    // Gets the EC2 on-demand hourly price and calculates the monthly cost
    public async ValueTask<CostEstimateResult> EstimateEc2Async(string instanceType, string region, int hours, CancellationToken cancellationToken = default)
    {
        using var pricing = factory.CreatePricingClient();
        var filters = new List<Filter>
        {
            new() { Field = "instanceType", Type = "TERM_MATCH", Value = instanceType },
            new() { Field = "regionCode", Type = "TERM_MATCH", Value = region },
            new() { Field = "operatingSystem", Type = "TERM_MATCH", Value = "Linux" },
            new() { Field = "tenancy", Type = "TERM_MATCH", Value = "Shared" },
            new() { Field = "capacityStatus", Type = "TERM_MATCH", Value = "Used" },
            new() { Field = "preInstalledSw", Type = "TERM_MATCH", Value = "NA" }
        };

        var hourlyUsd = await GetOnDemandPriceAsync(pricing, "AmazonEC2", filters, cancellationToken);
        var monthlyUsd = hourlyUsd * hours;

        return new CostEstimateResult("EC2", $"{instanceType} Linux OnDemand ({region})", hourlyUsd, monthlyUsd, hours);
    }

    // Gets the RDS PostgreSQL on-demand hourly price and calculates the monthly cost
    public async ValueTask<CostEstimateResult> EstimateRdsAsync(string engine, string instanceClass, string region, int hours, CancellationToken cancellationToken = default)
    {
        using var pricing = factory.CreatePricingClient();
        var databaseEngine = engine.Equals("aurora", StringComparison.OrdinalIgnoreCase) || engine.Equals("aurora-postgresql", StringComparison.OrdinalIgnoreCase)
            ? "Aurora PostgreSQL"
            : "PostgreSQL";

        var filters = new List<Filter>
        {
            new() { Field = "instanceType", Type = "TERM_MATCH", Value = instanceClass },
            new() { Field = "regionCode", Type = "TERM_MATCH", Value = region },
            new() { Field = "databaseEngine", Type = "TERM_MATCH", Value = databaseEngine },
            new() { Field = "deploymentOption", Type = "TERM_MATCH", Value = "Single-AZ" }
        };

        var hourlyUsd = await GetOnDemandPriceAsync(pricing, "AmazonRDS", filters, cancellationToken);
        var monthlyUsd = hourlyUsd * hours;

        return new CostEstimateResult("RDS", $"{instanceClass} {databaseEngine} OnDemand ({region})", hourlyUsd, monthlyUsd, hours);
    }

    private static async ValueTask<decimal> GetOnDemandPriceAsync(AmazonPricingClient pricing, string serviceCode, List<Filter> filters, CancellationToken cancellationToken)
    {
        var response = await pricing.GetProductsAsync(
            new GetProductsRequest
            {
                ServiceCode = serviceCode,
                Filters = filters,
                MaxResults = 1,
                FormatVersion = "aws_v1"
            },
            cancellationToken);

        if (response.PriceList.Count == 0)
        {
            throw new InvalidOperationException("No pricing data found for the specified parameters.");
        }

        var priceJson = JsonDocument.Parse(response.PriceList[0]);
        var terms = priceJson.RootElement.GetProperty("terms").GetProperty("OnDemand");

        foreach (var term in terms.EnumerateObject())
        {
            var priceDimensions = term.Value.GetProperty("priceDimensions");
            foreach (var dimension in priceDimensions.EnumerateObject())
            {
                var pricePerUnit = dimension.Value.GetProperty("pricePerUnit").GetProperty("USD").GetString();
                if (Decimal.TryParse(pricePerUnit, CultureInfo.InvariantCulture, out var price) && price > 0)
                {
                    return price;
                }
            }
        }

        throw new InvalidOperationException("Could not parse pricing data from AWS Pricing API response.");
    }
}
