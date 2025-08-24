using Classified.Shared.Infrastructure.RedisService;
using GeoService.Application.Services;
using GeoService.Domain.Abstractions;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddScoped<IMeteoGeoService, OpenMeteoGeoService>();


builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddScoped<IRedisService, RedisService>();

builder.Services.AddHttpClient<IMeteoGeoService, OpenMeteoGeoService>(client =>
{
    client.BaseAddress = new Uri("https://geocoding-api.open-meteo.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "GeoService/1.0"); // API требует User-Agent
});

builder.Services.AddHttpClient<IOpenCageGeoService, OpenCageGeoService>(client =>
{
    client.BaseAddress = new Uri("https://api.opencagedata.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    // не добавляем User-Agent — OpenCage не требует, но можно:
    // client.DefaultRequestHeaders.Add("User-Agent", "ClassifiedGeoService/1.0");
});

builder.Services.AddHttpClient<IGeoapifyGeoService, GeoapifyGeoService>(client =>
{
    client.BaseAddress = new Uri("https://api.geoapify.com");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "GeoService/1.0"); // допустимо
});

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
