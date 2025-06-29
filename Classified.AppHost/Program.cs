var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AuthService_API>("authservice-api");

builder.AddProject<Projects.UserService_API>("userservice-api");

builder.Build().Run();
