using AISAM.API.Middleware;
using AISAM.Common.Models;
using AISAM.Repositories;
using AISAM.Repositories.IRepositories;
using AISAM.Repositories.Repository;
using AISAM.Services.IServices;
using AISAM.Services.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using AISAM.API.Filters;
using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using AISAM.API.Validators;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using AISAM.API.Profiles;

// Load environment variables from .env file
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Load environment variables into configuration
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
}
else
{
    // Fallback to building connection string from parts
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
    var dbName = Environment.GetEnvironmentVariable("DB_NAME");
    var dbUser = Environment.GetEnvironmentVariable("DB_USER");
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
    
    if (!string.IsNullOrEmpty(dbHost))
    {
        builder.Configuration["ConnectionStrings:DefaultConnection"] = 
            $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
    }
}

// JWT environment overrides (align to Jwt:*)
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (!string.IsNullOrEmpty(jwtSecret))
{
    builder.Configuration["Jwt:SecretKey"] = jwtSecret;
}

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
if (!string.IsNullOrEmpty(jwtIssuer))
{
    builder.Configuration["Jwt:Issuer"] = jwtIssuer;
}

var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
if (!string.IsNullOrEmpty(jwtAudience))
{
    builder.Configuration["Jwt:Audience"] = jwtAudience;
}

var jwtAccessMinutes = Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_MINUTES");
if (!string.IsNullOrEmpty(jwtAccessMinutes))
{
    builder.Configuration["Jwt:AccessTokenExpirationMinutes"] = jwtAccessMinutes;
}

var jwtRefreshDays = Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_DAYS");
if (!string.IsNullOrEmpty(jwtRefreshDays))
{
    builder.Configuration["Jwt:RefreshTokenExpirationDays"] = jwtRefreshDays;
}

var facebookAppId = Environment.GetEnvironmentVariable("FACEBOOK_APP_ID");
if (!string.IsNullOrEmpty(facebookAppId))
{
    builder.Configuration["FacebookSettings:AppId"] = facebookAppId;
}

var facebookAppSecret = Environment.GetEnvironmentVariable("FACEBOOK_APP_SECRET");
if (!string.IsNullOrEmpty(facebookAppSecret))
{
    builder.Configuration["FacebookSettings:AppSecret"] = facebookAppSecret;
}

// Add services to the container.
builder.Services.AddControllers(options =>
{
        options.Filters.Add<ValidationFilter>();
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Register validators for DI (manual validation in controllers)
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestDtoValidator>();

// Add AutoMapper (scan profiles)
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Configure Facebook Settings
builder.Services.Configure<FacebookSettings>(
    builder.Configuration.GetSection("FacebookSettings"));

// Add Entity Framework
builder.Services.AddDbContext<AISAMContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HTTP Client for API calls
builder.Services.AddHttpClient();

// Add repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISocialAccountRepository, SocialAccountRepository>();
builder.Services.AddScoped<ISocialTargetRepository, SocialTargetRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();

// Add services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISocialService, SocialService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add provider services
builder.Services.AddScoped<IProviderService, FacebookProvider>();

// Background services removed for now

// Add JWT Authentication (uses Jwt:*)
var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "BookStore_Social_Media_Secret_Key_2025_Very_Long_Secret";
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AISAM.API",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AISAM.Client",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Disable automatic 400 for model validation to allow custom GenericResponse
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Add CORS policy (supports credentials and specific origins)
var configuredOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

// Fallback to env var CORS_ALLOWED_ORIGINS (comma-separated) if config is empty
if (configuredOrigins.Length == 0)
{
    var corsEnv = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
    if (!string.IsNullOrWhiteSpace(corsEnv))
    {
        configuredOrigins = corsEnv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        corsBuilder =>
        {
            if (configuredOrigins.Length > 0)
            {
                corsBuilder
                    .WithOrigins(configuredOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
            else
            {
                // Safe default for development if no origins configured
                corsBuilder
                    .WithOrigins("http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "AISAM Social Media API", 
        Version = "v1",
        Description = "API for managing social media integration and posting"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme. 
                      Enter your token in the text input below.
                      Example: '12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// Add global exception handling middleware
app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();