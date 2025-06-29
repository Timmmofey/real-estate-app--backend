using Microsoft.EntityFrameworkCore;
using UserService.API.Extensions;
using UserService.Persistance.PostgreSQL;
using FluentValidation.AspNetCore;
using FluentValidation;
using UserService.Application;
using Classified.Shared.Infrastructure.S3.Extensions;
using UserService.Infrastructure;
using UserService.Domain.Abstactions;
using Classified.Shared.Extensions;
using UserService.Infrastructure.Kafka;
using Classified.Shared.Infrastructure.EmailService;
using UserService.Infrastructure.AuthService;
using StackExchange.Redis;
using UserService.Infrastructure.RedisService;


var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//UserService
builder.Services.AddHttpClient<IAuthServiceClient, AuthServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AuthService:BaseUrl"]!);
});

//Kafka
builder.Services.AddScoped<IKafkaProducer, KafkaProducer>();

builder.Services.AddDbContext<UserServicePostgreDbContext>(
    options =>
    {
        options.UseNpgsql(configuration.GetConnectionString(nameof(UserServicePostgreDbContext)));
    }
);
builder.Services.AddSufyS3Storage(builder.Configuration);

//
builder.Services.AddApplicationServices();
builder.Services.AddApplicationRepositories();
//
builder.Services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
//builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddEmailService(builder.Configuration);

builder.Services.AddJwtAuthentication(configuration);

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddScoped<IRedisService, RedisService>();

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Добавляем мидлвар для глобальной обработки ошибок
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
