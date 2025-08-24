using AuthService.Domain.Abstactions;
using AuthService.Infrastructure.Jwt;
using AuthService.Infrastructure.Kafka;
using AuthService.Infrastructure.UserService;
using AuthService.Persistance;
using AuthService.Persistance.Repositories;
using Classified.Shared.Extensions;
using FluentValidation.AspNetCore;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UAParser;
using AuthService.Application;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var configuration = builder.Configuration;


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<KafkaConsumer>();


//UserService
builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["UserService:BaseUrl"]!);
});
builder.Services.AddHttpContextAccessor();
//PostgreDb
builder.Services.AddDbContext<AuthServicePostgreDbContext>(
    options =>
    {
        options.UseNpgsql(configuration.GetConnectionString(nameof(AuthServicePostgreDbContext)));
    }
);
// HttpContextAccessor нужны для User-Agent и IP
builder.Services.AddHttpContextAccessor();
// JWT, UAParser
builder.Services.AddSingleton<IJwtProvider, JwtProvider>();
builder.Services.AddSingleton(Parser.GetDefault());
// AuthService
builder.Services.AddScoped<IAuthService, AuthService.Application.Services.AuthService>();
builder.Services.AddScoped<IRefreshTokenRepository, SessionRepository>();
//
builder.Services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddJwtAuthentication(configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

    options.AddPolicy("AllowUserService", policy =>
    {
        policy.WithOrigins("http://localhost:5120")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



// Добавляем мидлвар для глобальной обработки ошибок
//app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();



app.UseCors("AllowFrontend");

app.Run();
