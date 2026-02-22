using AuthService.Domain.Abstactions;
using AuthService.Infrastructure.Jwt;
using AuthService.Infrastructure.Kafka;
using AuthService.Infrastructure.UserService;
using AuthService.Persistance;
using AuthService.Persistance.Repositories;
using Classified.Shared.Extensions;
using Classified.Shared.Extensions.Auth;
using FluentValidation.AspNetCore;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UAParser;
using AuthService.Application;
using Classified.Shared.Infrastructure.RedisService;
using StackExchange.Redis;
using Classified.Shared.Infrastructure.EmailService;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

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
builder.Services.AddEmailService(configuration);

//JwtAuth
builder.Services.AddJwtAuthentication(configuration);

//Cors
builder.Services.AddDefaultCors();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogle(options =>
    {
        options.ClientId = configuration["Google:ClientId"];
        options.ClientSecret = configuration["Google:ClientSecret"];

        options.Scope.Add("profile");
        options.Scope.Add("email");

        options.SaveTokens = true;

        options.Events.OnCreatingTicket = ctx =>
        {
            // тут можно логировать или добавлять кастомные claim’ы
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            var redirectUri = context.RedirectUri;

            // добавляем prompt=select_account
            redirectUri += "&prompt=select_account";
            context.Response.Redirect(redirectUri);
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

app.UseAppLocalization();



app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();



app.UseCors("AllowFrontend");

app.Run();
