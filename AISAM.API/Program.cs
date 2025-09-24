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
using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using AISAM.API.Validators;
using Microsoft.AspNetCore.Mvc;

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

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (!string.IsNullOrEmpty(jwtSecret))
{
    builder.Configuration["JwtSettings:SecretKey"] = jwtSecret;
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
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Register validators for DI (manual validation in controllers)
builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserDtoValidator>();

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

// Add provider services
builder.Services.AddScoped<IProviderService, FacebookProvider>();

// Background services removed for now

// Add JWT Authentication
var secretKey = builder.Configuration["JwtSettings:SecretKey"] ?? "BookStore_Social_Media_Secret_Key_2025_Very_Long_Secret";
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "AISAM.API",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "AISAM.Client",
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

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", 
        corsBuilder => corsBuilder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "AISAM Social Media API", 
        Version = "v1",
        Description = "API for managing social media integration and posting"
    });
    
    // Configure Bearer token authentication for Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // OData removed
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// Add global exception handling middleware
app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();