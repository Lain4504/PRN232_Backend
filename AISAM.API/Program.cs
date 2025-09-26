using AISAM.API.Middleware;
using AISAM.Common.Models;
using AISAM.Repositories;
using AISAM.Repositories.IRepositories;
using AISAM.Repositories.Repository;
using AISAM.Services.IServices;
using AISAM.Services.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using AISAM.API.Filters;
using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using AISAM.API.Validators;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using AISAM.API.Profiles;
using Supabase;

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

// Register Supabase client singleton for future auth/storage usage
var supabaseUrl = builder.Configuration["Supabase:Url"]
                  ?? Environment.GetEnvironmentVariable("SUPABASE_URL");
var supabaseKey = builder.Configuration["Supabase:AnonKey"]
                  ?? builder.Configuration["Supabase:ServiceKey"]
                  ?? Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY")
                  ?? Environment.GetEnvironmentVariable("SUPABASE_KEY");
if (!string.IsNullOrWhiteSpace(supabaseUrl) && !string.IsNullOrWhiteSpace(supabaseKey))
{
    builder.Services.AddSingleton(provider =>
    {
        var opts = new Supabase.SupabaseOptions
        {
            AutoConnectRealtime = true
        };
        var client = new Client(supabaseUrl, supabaseKey, opts);
        client.InitializeAsync().GetAwaiter().GetResult();
        return client;
    });
}

// Add Entity Framework - Supabase Postgres via Npgsql
// Expect connection string from env or appsettings ConnectionStrings:DefaultConnection (Supabase URI)
builder.Services.AddDbContext<AISAMContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HTTP Client for API calls
builder.Services.AddHttpClient();

// Add repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISocialAccountRepository, SocialAccountRepository>();
builder.Services.AddScoped<ISocialTargetRepository, SocialTargetRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();

// Add services (auth services removed; Supabase auth will be integrated later)
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISocialService, SocialService>();
builder.Services.AddScoped<IPostService, PostService>();

// Add provider services
builder.Services.AddScoped<IProviderService, FacebookProvider>();

// Background services removed for now

// Remove JWT auth for now; Supabase auth to be integrated later
// builder.Services.AddAuthentication(...)
// builder.Services.AddAuthorization();

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

// Auth temporarily disabled pending Supabase integration
// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

app.Run();