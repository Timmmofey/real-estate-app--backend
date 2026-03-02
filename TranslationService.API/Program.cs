using Classified.Shared.Extensions;
using Classified.Shared.Extensions.ServerJwtAuth;
using Classified.Shared.Infrastructure.MicroserviceJwt;
using Classified.Shared.Infrastructure.RedisService;
using StackExchange.Redis;
using TranslationService.Application.Services;
using TranslationService.Domain.Abstractions;
using TranslationService.Infrastructure.GoogleTranslate;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddScoped<IRedisService, RedisService>();

builder.Services.AddScoped<IGoogleTranslationService, GoogleTranslationService>();
builder.Services.AddHttpClient<IGoogleTranslateClient, GoogleTranslateClient>();

//Cors
builder.Services.AddDefaultCors();

//Server JWT
builder.Services.AddServerJwtAuthentication(builder.Configuration);
builder.Services.AddSingleton<IMicroserviceJwtProvider, MicroserviceJwtProvider>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();


app.MapControllers();

app.Run();
