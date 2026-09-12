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

    // EC2 OnDemand 時間単価を取得して月額を計算する。
    public async ValueTask<CostEstimateResult> EstimateEc2Async(string instanceType, string region, int hours)
    {
        using var pricing = factory.CreatePricingClient();
        var filters = new List<Filter>
        {
            new() { Field = "instanceType", Type = "TERM_MATCH", Value = instanceType },
            new() { Field = "location", Type = "TERM_MATCH", Value = RegionToLocation(region) },
            new() { Field = "operatingSystem", Type = "TERM_MATCH", Value = "Linux" },
            new() { Field = "tenancy", Type = "TERM_MATCH", Value = "Shared" },
            new() { Field = "capacityStatus", Type = "TERM_MATCH", Value = "Used" },
            new() { Field = "preInstalledSw", Type = "TERM_MATCH", Value = "NA" }
        };

        var hourlyUsd = await GetOnDemandPriceAsync(pricing, "AmazonEC2", filters);
        var monthlyUsd = hourlyUsd * hours;

        return new CostEstimateResult("EC2", $"{instanceType} Linux OnDemand ({region})", hourlyUsd, monthlyUsd, hours);
    }

    // RDS PostgreSQL OnDemand 時間単価を取得して月額を計算する。
    public async ValueTask<CostEstimateResult> EstimateRdsAsync(string engine, string instanceClass, string region, int hours)
    {
        using var pricing = factory.CreatePricingClient();
        var databaseEngine = engine.Equals("aurora", StringComparison.OrdinalIgnoreCase) || engine.Equals("aurora-postgresql", StringComparison.OrdinalIgnoreCase)
            ? "Aurora PostgreSQL"
            : "PostgreSQL";

        var filters = new List<Filter>
        {
            new() { Field = "instanceType", Type = "TERM_MATCH", Value = instanceClass },
            new() { Field = "location", Type = "TERM_MATCH", Value = RegionToLocation(region) },
            new() { Field = "databaseEngine", Type = "TERM_MATCH", Value = databaseEngine },
            new() { Field = "deploymentOption", Type = "TERM_MATCH", Value = "Single-AZ" }
        };

        var hourlyUsd = await GetOnDemandPriceAsync(pricing, "AmazonRDS", filters);
        var monthlyUsd = hourlyUsd * hours;

        return new CostEstimateResult("RDS", $"{instanceClass} {databaseEngine} OnDemand ({region})", hourlyUsd, monthlyUsd, hours);
    }

    private static async ValueTask<decimal> GetOnDemandPriceAsync(AmazonPricingClient pricing, string serviceCode, List<Filter> filters)
    {
        var response = await pricing.GetProductsAsync(
            new GetProductsRequest
            {
                ServiceCode = serviceCode,
                Filters = filters,
                MaxResults = 1,
                FormatVersion = "aws_v1"
            });

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
                if (decimal.TryParse(pricePerUnit, CultureInfo.InvariantCulture, out var price) && price > 0)
                {
                    return price;
                }
            }
        }

        throw new InvalidOperationException("Could not parse pricing data from AWS Pricing API response.");
    }

    private static string RegionToLocation(string region) => region switch
    {
        "us-east-1" => "US East (N. Virginia)",
        "us-east-2" => "US East (Ohio)",
        "us-west-1" => "US West (N. California)",
        "us-west-2" => "US West (Oregon)",
        "ap-northeast-1" => "Asia Pacific (Tokyo)",
        "ap-northeast-2" => "Asia Pacific (Seoul)",
        "ap-northeast-3" => "Asia Pacific (Osaka)",
        "ap-southeast-1" => "Asia Pacific (Singapore)",
        "ap-southeast-2" => "Asia Pacific (Sydney)",
        "ap-south-1" => "Asia Pacific (Mumbai)",
        "eu-west-1" => "Europe (Ireland)",
        "eu-west-2" => "Europe (London)",
        "eu-central-1" => "Europe (Frankfurt)",
        "ca-central-1" => "Canada (Central)",
        "sa-east-1" => "South America (Sao Paulo)",
        _ => region
    };
}
