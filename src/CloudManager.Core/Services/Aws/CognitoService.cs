namespace CloudManager.Services.Aws;

using Amazon.CognitoIdentityProvider.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Cognito;

public sealed class CognitoService
{
    private readonly AwsClientFactory factory;

    public CognitoService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    public async ValueTask<List<UserPoolInfo>> ListUserPoolsAsync(CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateCognitoClient();
        var results = new List<UserPoolInfo>();
        string? nextToken = null;
        do
        {
            var response = await client.ListUserPoolsAsync(
                new ListUserPoolsRequest
                {
                    MaxResults = 60,
                    NextToken = nextToken
                },
                cancellationToken);
            foreach (var pool in response.UserPools ?? [])
            {
                results.Add(new UserPoolInfo(
                    pool.Id ?? string.Empty,
                    pool.Name ?? string.Empty,
                    pool.CreationDate));
            }

            nextToken = response.NextToken;
        }
        while (!String.IsNullOrEmpty(nextToken));
        return results;
    }

    public async ValueTask<List<CognitoUserInfo>> ListUsersAsync(string userPoolId, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateCognitoClient();
        var results = new List<CognitoUserInfo>();
        string? paginationToken = null;
        do
        {
            var response = await client.ListUsersAsync(
                new ListUsersRequest
                {
                    UserPoolId = userPoolId,
                    PaginationToken = paginationToken
                },
                cancellationToken);
            foreach (var user in response.Users ?? [])
            {
                var email = user.Attributes?.FirstOrDefault(a => a.Name == "email")?.Value;
                results.Add(new CognitoUserInfo(
                    user.Username ?? string.Empty,
                    user.UserStatus?.Value ?? string.Empty,
                    user.Enabled.GetValueOrDefault(),
                    user.UserCreateDate,
                    email));
            }

            paginationToken = response.PaginationToken;
        }
        while (!String.IsNullOrEmpty(paginationToken));
        return results;
    }

    public async ValueTask AdminResetPasswordAsync(
        string userPoolId,
        string username,
        CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateCognitoClient();
        await client.AdminResetUserPasswordAsync(
            new AdminResetUserPasswordRequest
            {
                UserPoolId = userPoolId,
                Username = username
            },
            cancellationToken);
    }

    public async ValueTask AdminSetTempPasswordAsync(
        string userPoolId,
        string username,
        string tempPassword,
        CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateCognitoClient();
        await client.AdminSetUserPasswordAsync(
            new AdminSetUserPasswordRequest
            {
                UserPoolId = userPoolId,
                Username = username,
                Password = tempPassword,
                Permanent = false
            },
            cancellationToken);
    }
}
