var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AuthService_API>("authservice-api");

builder.AddProject<Projects.UserService_API>("userservice-api");

builder.AddProject<Projects.GeoService_API>("geoservice-api");

//builder.AddProject<Projects.RealEstateService_API>("realestateservice-api");

builder.AddProject<Projects.TranslationService_API>("translationservice-api");

builder.Build().Run();
