using AuthService.Application;
using AuthService.Domain.Abstactions;
using AuthService.Infrastructure.IpGeoService;
using AuthService.Infrastructure.Jwt;
using AuthService.Infrastructure.Kafka;
using AuthService.Infrastructure.UserService;
using AuthService.Persistance;
using AuthService.Persistance.Repositories;
using Classified.Shared.Extensions;
using Classified.Shared.Extensions.Auth;
using Classified.Shared.Extensions.ServerJwtAuth;
using Classified.Shared.Infrastructure.EmailService;
using Classified.Shared.Infrastructure.MicroserviceJwt;
using Classified.Shared.Infrastructure.RedisService;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UAParser;

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

//Localization
builder.Services.AddAppLocalization();

//Redis
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));

//Email service
builder.Services.AddEmailService();

//Ip GeoService
builder.Services.AddSingleton<IIpGeoService, IpGeoService>();

//Кеш для Ip GeoService
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 150_000; 
});

//JwtAuth
builder.Services.AddJwtAuthentication(configuration);
builder.Services.AddServerJwtAuthentication(builder.Configuration);
builder.Services.AddSingleton<IMicroserviceJwtProvider, MicroserviceJwtProvider>();

//Cors
builder.Services.AddDefaultCors();

builder.Services.AddAuthentication()
    .AddCookie("GoogleCookieScheme", options =>
    {
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // API не редиректим, отдаем 401
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    })
    .AddGoogle("GoogleAuthScheme", options =>
    {
        options.SignInScheme = "GoogleCookieScheme";
        options.ClientId = configuration["Google:ClientId"]!;
        options.ClientSecret = configuration["Google:ClientSecret"]!;
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.SaveTokens = true;
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.Response.Redirect(context.RedirectUri + "&prompt=select_account");
            return Task.CompletedTask;
        };
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

app.UseRouting();

app.UseAppLocalization();



app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();




app.Run();
