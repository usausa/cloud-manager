// ReSharper disable StringLiteralTypo
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.CloudManager_Host>("cloudmanager")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
