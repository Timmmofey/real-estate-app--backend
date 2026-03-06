using Classified.Shared.Extensions;
using Classified.Shared.Extensions.Auth;
using Classified.Shared.Extensions.ErrorHandler;
using Classified.Shared.Extensions.ServerJwtAuth;
using Classified.Shared.Infrastructure.EmailService;
using Classified.Shared.Infrastructure.MicroserviceJwt;
using Classified.Shared.Infrastructure.RedisService;
using Classified.Shared.Infrastructure.S3.Extensions;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;
using UserService.API.Extensions;
using UserService.Application;
using UserService.Domain.Abstactions;
using UserService.Infrastructure;
using UserService.Infrastructure.AuthService;
using UserService.Infrastructure.GeoService;
using UserService.Infrastructure.Kafka;
using UserService.Persistance.PostgreSQL;



var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.

//Localization
builder.Services.AddAppLocalization();

//builder.Services.AddControllers();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Устанавливает формат имен свойств объектов в JSON в camelCase.
        // Например:
        // C# -> public string PhoneNumber { get; set; }
        // JSON -> "phoneNumber"
        //
        // Без этого клиент получал бы "PhoneNumber".
        options.JsonSerializerOptions.PropertyNamingPolicy =
            JsonNamingPolicy.CamelCase;

        // Устанавливает формат ключей словаря (Dictionary<string, T>) в camelCase.
        // ВАЖНО для наших ошибок валидации:
        //
        // errors.Add(nameof(CreatePersonUserDto.PhoneNumber), ...)
        //
        // В C# ключ будет "PhoneNumber",
        // а в JSON станет "phoneNumber".
        //
        // Без этой настройки ключи словаря останутся в PascalCase.
        options.JsonSerializerOptions.DictionaryKeyPolicy =
            JsonNamingPolicy.CamelCase;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Для мидлвара глобальной обработки ошибок
builder.Services.AddApiErrorHandling();

//UserService
builder.Services.AddHttpClient<IAuthServiceClient, AuthServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AuthService:BaseUrl"]!);
});

//GeoService
builder.Services.AddHttpClient<IGeoServiceClient, GeoServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["GeoService:BaseUrl"]!);
});


//Kafka
builder.Services.AddScoped<IKafkaProducer, KafkaProducer>();

builder.Services.AddDbContext<UserServicePostgreDbContext>(
    options =>
    {
        options.UseNpgsql(configuration.GetConnectionString(nameof(UserServicePostgreDbContext)));
    }
);
builder.Services.AddSufyS3Storage();

//
builder.Services.AddApplicationServices();
builder.Services.AddApplicationRepositories();
//
builder.Services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
//builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddEmailService();

builder.Services.AddJwtAuthentication(configuration);
builder.Services.AddServerJwtAuthentication(builder.Configuration);
builder.Services.AddSingleton<IMicroserviceJwtProvider, MicroserviceJwtProvider>();


builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddScoped<IRedisService, RedisService>();


//Cors
builder.Services.AddDefaultCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Добавляем мидлвар для глобальной обработки ошибок
//app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseRouting();

app.UseAppLocalization();



app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();



app.Run();