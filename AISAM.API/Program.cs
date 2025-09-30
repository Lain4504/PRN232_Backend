using System.Security.Claims;
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
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AISAM.Services;

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

// Register validators from API assembly
builder.Services.AddValidatorsFromAssemblyContaining<AISAM.API.Validators.CreateContentRequestValidator>();

// Enable FluentValidation automatic model validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();


// Configure Facebook Settings
builder.Services.Configure<FacebookSettings>(
    builder.Configuration.GetSection("FacebookSettings"));

// Register Supabase client singleton for future auth/storage usage
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");
if (!string.IsNullOrWhiteSpace(supabaseUrl) && !string.IsNullOrWhiteSpace(supabaseKey))
{
    builder.Services.AddSingleton(_ =>
    {
        var opts = new SupabaseOptions
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
builder.Services.AddDbContext<AisamContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HTTP Client for API calls
builder.Services.AddHttpClient();

// Add repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISocialAccountRepository, SocialAccountRepository>();
builder.Services.AddScoped<ISocialIntegrationRepository, SocialIntegrationRepository>();
builder.Services.AddScoped<IContentRepository, ContentRepository>();

// Add services 
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISocialService, SocialService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<SupabaseStorageService>();
builder.Services.AddHostedService<BucketInitializerService>();

// Add provider services
builder.Services.AddScoped<IProviderService, FacebookProvider>();

var jwksUri = $"{supabaseUrl!.TrimEnd('/')}/auth/v1/.well-known/jwks.json";

// Fetch JWKS 1 lần khi app start
using var http = new HttpClient();
var jwksJson = await http.GetStringAsync(jwksUri);
var jwks = new JsonWebKeySet(jwksJson);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.IncludeErrorDetails = true;
        options.RequireHttpsMetadata = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl.TrimEnd('/')}/auth/v1",

            ValidateAudience = true,
            ValidAudience = "authenticated",

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),

            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = jwks.Keys,   // 👈 load trực tiếp từ Supabase
            ValidAlgorithms = new[] { SecurityAlgorithms.EcdsaSha256 }
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("Auth failed: " + ctx.Exception);
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                Console.WriteLine("Token OK for user: " +
                                  ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier));
                return Task.CompletedTask;
            }
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

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();