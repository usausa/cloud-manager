namespace CloudManager.Infrastructure.Aws;

using Amazon;
using Amazon.APIGateway;
using Amazon.CertificateManager;
using Amazon.CloudFront;
using Amazon.CloudWatch;
using Amazon.CloudWatchLogs;
using Amazon.CognitoIdentityProvider;
using Amazon.DynamoDBv2;
using Amazon.EC2;
using Amazon.ECR;
using Amazon.ECS;
using Amazon.ElasticLoadBalancingV2;
using Amazon.EventBridge;
using Amazon.Lambda;
using Amazon.Pricing;
using Amazon.RDS;
using Amazon.Route53;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SecretsManager;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using Amazon.SQS;

// Creates AWS clients, resolving credentials and region on every call
public sealed class AwsClientFactory
{
    private readonly Func<(AWSCredentials Credentials, RegionEndpoint Region)> resolver;

    public AwsClientFactory(Func<(AWSCredentials Credentials, RegionEndpoint Region)> resolver)
    {
        this.resolver = resolver;
    }

    // Creates a factory bound to a fixed profile and region (for job execution)
    public static AwsClientFactory Create(string profileName, string regionName) =>
        new(() => CredentialResolver.Resolve(profileName, regionName));

    private (AWSCredentials Credentials, RegionEndpoint Region) Resolve() => resolver();

    public AmazonEC2Client CreateEc2Client()
    {
        var (credentials, region) = Resolve();
        return new AmazonEC2Client(credentials, region);
    }

    public AmazonRDSClient CreateRdsClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonRDSClient(credentials, region);
    }

    public AmazonS3Client CreateS3Client()
    {
        var (credentials, region) = Resolve();
        return new AmazonS3Client(credentials, region);
    }

    public AmazonLambdaClient CreateLambdaClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonLambdaClient(credentials, region);
    }

    public AmazonDynamoDBClient CreateDynamoDbClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonDynamoDBClient(credentials, region);
    }

    public AmazonECSClient CreateEcsClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonECSClient(credentials, region);
    }

    public AmazonSQSClient CreateSqsClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonSQSClient(credentials, region);
    }

    public AmazonCloudFrontClient CreateCloudFrontClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonCloudFrontClient(credentials, region);
    }

    public AmazonCloudWatchClient CreateCloudWatchClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonCloudWatchClient(credentials, region);
    }

    public AmazonSimpleSystemsManagementClient CreateSsmClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonSimpleSystemsManagementClient(credentials, region);
    }

    // Pricing API is only available in us-east-1
    public AmazonPricingClient CreatePricingClient()
    {
        var (credentials, _) = Resolve();
        return new AmazonPricingClient(credentials, RegionEndpoint.USEast1);
    }

    public AmazonCloudWatchLogsClient CreateCloudWatchLogsClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonCloudWatchLogsClient(credentials, region);
    }

    public AmazonSecretsManagerClient CreateSecretsManagerClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonSecretsManagerClient(credentials, region);
    }

    public AmazonSimpleNotificationServiceClient CreateSnsClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonSimpleNotificationServiceClient(credentials, region);
    }

    public AmazonECRClient CreateEcrClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonECRClient(credentials, region);
    }

    public AmazonElasticLoadBalancingV2Client CreateElbClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonElasticLoadBalancingV2Client(credentials, region);
    }

    // Route53 uses the global endpoint
    public AmazonRoute53Client CreateRoute53Client()
    {
        var (credentials, _) = Resolve();
        return new AmazonRoute53Client(credentials, RegionEndpoint.USEast1);
    }

    public AmazonCertificateManagerClient CreateAcmClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonCertificateManagerClient(credentials, region);
    }

    public AmazonAPIGatewayClient CreateApiGatewayClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonAPIGatewayClient(credentials, region);
    }

    public AmazonEventBridgeClient CreateEventBridgeClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonEventBridgeClient(credentials, region);
    }

    public AmazonCognitoIdentityProviderClient CreateCognitoClient()
    {
        var (credentials, region) = Resolve();
        return new AmazonCognitoIdentityProviderClient(credentials, region);
    }
}
