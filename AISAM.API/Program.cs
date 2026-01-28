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
using AISAM.Repositories.Repositories;
using AISAM.Services.Helper;
using AISAM.API.Validators;
using AISAM.Common.Dtos.Request;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;
using AISAM.Common.Config;
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

var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
if (!string.IsNullOrEmpty(geminiApiKey))
{
    builder.Configuration["Gemini:ApiKey"] = geminiApiKey;
}

var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL");
if (!string.IsNullOrEmpty(frontendBaseUrl))
{
    builder.Configuration["FrontendSettings:BaseUrl"] = frontendBaseUrl;
}

// PayOS configuration
var payosClientId = Environment.GetEnvironmentVariable("PAYOS_CLIENT_ID");
if (!string.IsNullOrEmpty(payosClientId))
{
    builder.Configuration["PayOS:ClientId"] = payosClientId;
}

var payosApiKey = Environment.GetEnvironmentVariable("PAYOS_API_KEY");
if (!string.IsNullOrEmpty(payosApiKey))
{
    builder.Configuration["PayOS:ApiKey"] = payosApiKey;
}

var payosChecksumKey = Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY");
if (!string.IsNullOrEmpty(payosChecksumKey))
{
    builder.Configuration["PayOS:ChecksumKey"] = payosChecksumKey;
}

// Facebook sandbox configuration override
var facebookUseSandbox = Environment.GetEnvironmentVariable("FACEBOOK_USE_SANDBOX");
if (!string.IsNullOrEmpty(facebookUseSandbox))
{
    builder.Configuration["FacebookSettings:UseSandbox"] = facebookUseSandbox;
}

var facebookSandboxAccessToken = Environment.GetEnvironmentVariable("FACEBOOK_SANDBOX_ACCESS_TOKEN");
if (!string.IsNullOrEmpty(facebookSandboxAccessToken))
{
    builder.Configuration["FacebookSettings:Sandbox:AccessToken"] = facebookSandboxAccessToken;
}

// Google OAuth configuration override
var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
if (!string.IsNullOrEmpty(googleClientId))
{
    builder.Configuration["GoogleSettings:ClientId"] = googleClientId;
}

var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
if (!string.IsNullOrEmpty(googleClientSecret))
{
    builder.Configuration["GoogleSettings:ClientSecret"] = googleClientSecret;
}

// JWT configuration override
var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (!string.IsNullOrEmpty(jwtSecretKey))
{
    builder.Configuration["JwtSettings:SecretKey"] = jwtSecretKey;
}

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
if (!string.IsNullOrEmpty(jwtIssuer))
{
    builder.Configuration["JwtSettings:Issuer"] = jwtIssuer;
}

var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
if (!string.IsNullOrEmpty(jwtAudience))
{
    builder.Configuration["JwtSettings:Audience"] = jwtAudience;
}

// Email configuration override
var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
if (!string.IsNullOrEmpty(smtpHost))
{
    builder.Configuration["EmailSettings:SmtpHost"] = smtpHost;
}

var smtpPort = Environment.GetEnvironmentVariable("SMTP_PORT");
if (!string.IsNullOrEmpty(smtpPort))
{
    builder.Configuration["EmailSettings:SmtpPort"] = smtpPort;
}

var smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME");
if (!string.IsNullOrEmpty(smtpUsername))
{
    builder.Configuration["EmailSettings:SmtpUsername"] = smtpUsername;
}

var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
if (!string.IsNullOrEmpty(smtpPassword))
{
    builder.Configuration["EmailSettings:SmtpPassword"] = smtpPassword;
}

var fromEmail = Environment.GetEnvironmentVariable("FROM_EMAIL");
if (!string.IsNullOrEmpty(fromEmail))
{
    builder.Configuration["EmailSettings:FromEmail"] = fromEmail;
}

// Google Project Configuration
var googleProjectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");
if (!string.IsNullOrEmpty(googleProjectId))
{
    builder.Configuration["GoogleCloud:ProjectId"] = googleProjectId;
}

var googleLocation = Environment.GetEnvironmentVariable("GOOGLE_LOCATION");
if (!string.IsNullOrEmpty(googleLocation))
{
    builder.Configuration["GoogleCloud:Location"] = googleLocation;
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

// Enable FluentValidation automatic model validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// Configure Email Settings
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Configure Google Settings
builder.Services.Configure<GoogleSettings>(
    builder.Configuration.GetSection("GoogleSettings"));

// Configure Facebook Settings
builder.Services.Configure<FacebookSettings>(
    builder.Configuration.GetSection("FacebookSettings"));

// Configure Frontend Settings
builder.Services.Configure<FrontendSettings>(
    builder.Configuration.GetSection("FrontendSettings"));

// Configure Gemini Settings
builder.Services.Configure<GeminiSettings>(
    builder.Configuration.GetSection("Gemini"));

// Configure JWT Settings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// Register Supabase client for storage only (not for authentication)
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");
if (!string.IsNullOrWhiteSpace(supabaseUrl) && !string.IsNullOrWhiteSpace(supabaseKey))
{
    builder.Services.AddSingleton(_ =>
    {
        var opts = new SupabaseOptions
        {
            AutoConnectRealtime = false // We only need storage, not realtime
        };
        var client = new Client(supabaseUrl, supabaseKey, opts);
        client.InitializeAsync().GetAwaiter().GetResult();
        return client;
    });
}

// Add Entity Framework - PostgreSQL via Npgsql
// ✅ Thay vì dùng connection string trực tiếp, ta dùng NpgsqlDataSourceBuilder
var dataSourceBuilder = new NpgsqlDataSourceBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection")
);

// 👇 Thêm dòng này để bật dynamic JSON serialization
dataSourceBuilder.EnableDynamicJson();

var dataSource = dataSourceBuilder.Build();

// ✅ Đăng ký DbContext với data source vừa tạo
builder.Services.AddDbContext<AisamContext>(options =>
    options.UseNpgsql(dataSource)
);

// Add HTTP Client for API calls
builder.Services.AddHttpClient();

// Add repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<ISocialAccountRepository, SocialAccountRepository>();
builder.Services.AddScoped<ISocialIntegrationRepository, SocialIntegrationRepository>();
builder.Services.AddScoped<IContentRepository, ContentRepository>();
builder.Services.AddScoped<IAiGenerationRepository, AiGenerationRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IContentCalendarRepository, ContentCalendarRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IApprovalRepository, ApprovalRepository>();
builder.Services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ITeamBrandRepository, TeamBrandRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IAdCampaignRepository, AdCampaignRepository>();
builder.Services.AddScoped<IAdSetRepository, AdSetRepository>();
builder.Services.AddScoped<IAdCreativeRepository, AdCreativeRepository>();
builder.Services.AddScoped<IAdRepository, AdRepository>();
builder.Services.AddScoped<IPerformanceReportRepository, PerformanceReportRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

// Add services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISocialService, SocialService>();
builder.Services.AddScoped<IContentService, ContentService>();
// Note: SupabaseStorageService is still being used for file storage
// You may want to migrate to a different storage solution (Azure Blob, AWS S3, etc.)
// For now, keeping Supabase for storage only, not authentication
builder.Services.AddScoped<SupabaseStorageService>();
builder.Services.AddHostedService<BucketInitializerService>();
builder.Services.AddHostedService<NotificationCleanupService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ITeamMemberService, TeamMemberService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
// Add scheduled posting services
builder.Services.AddScoped<IScheduledPostingService, ScheduledPostingService>();
builder.Services.AddHostedService<ScheduledPostingBackgroundService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAdCampaignService, AdCampaignService>();
builder.Services.AddScoped<IAdSetService, AdSetService>();
builder.Services.AddScoped<IAdCreativeService, AdCreativeService>();
builder.Services.AddScoped<IAdService, AdService>();
builder.Services.AddScoped<IAdQuotaService, AdQuotaService>();
builder.Services.AddScoped<IFacebookMarketingApiService, FacebookMarketingApiService>();
// Add provider services
builder.Services.AddScoped<IProviderService, FacebookProvider>();
builder.Services.AddScoped<IProviderService, GoogleProvider>();
builder.Services.AddScoped<IPaymentService, PayOSPaymentService>();

builder.Services.AddSingleton<RolePermissionConfig>();

// Add validators
builder.Services.AddScoped<FluentValidation.IValidator<CreateAdCampaignRequest>, CreateAdCampaignRequestValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<CreateAdSetRequest>, CreateAdSetRequestValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<CreateAdCreativeRequest>, CreateAdCreativeRequestValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<CreateAdCreativeFromContentRequest>, CreateAdCreativeFromContentRequestValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<CreateAdCreativeFromFacebookPostRequest>, CreateAdCreativeFromFacebookPostRequestValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<CreateAdRequest>, CreateAdRequestValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<UpdateAdStatusRequest>, UpdateAdStatusRequestValidator>();
builder.Services.AddScoped<PublishRequestValidator>();

// Add subscription validation service
builder.Services.AddScoped<ISubscriptionValidationService, SubscriptionValidationService>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured in appsettings.json");
}

var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],

        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],

        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5),

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            Console.WriteLine("JWT Auth failed: " + ctx.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = ctx =>
        {
            var userId = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"JWT Token validated for user: {userId}");
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

var configuredOrigins = new List<string>();

// 1. Load from Environment Variable
var corsEnv = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
if (!string.IsNullOrWhiteSpace(corsEnv))
{
    configuredOrigins.AddRange(corsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

// 2. Load from Configuration (appsettings.json)
var corsConfig = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsConfig != null)
{
    configuredOrigins.AddRange(corsConfig);
}

// 3. Load from FRONTEND_BASE_URL
var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL");
if (!string.IsNullOrWhiteSpace(frontendUrl))
{
    configuredOrigins.Add(frontendUrl.TrimEnd('/'));
}

// 4. Deduplicate
var finalOrigins = configuredOrigins.Distinct().ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        corsBuilder =>
        {
            if (finalOrigins.Length > 0)
            {
                corsBuilder
                    .WithOrigins(finalOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetIsOriginAllowedToAllowWildcardSubdomains(); // Useful for subdomains
            }
            else
            {
                // Fallback for development if no origins configured
                corsBuilder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
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

// 1. MUST BE FIRST: CORS must handle preflight and wrap the entire pipeline
app.UseCors("CorsPolicy");

// 2. Swagger/Redirection
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

// 3. Exception Handling (After CORS so error responses get CORS headers)
app.UseMiddleware<ExceptionHandlerMiddleware>();

// 4. Auth & Controllers
app.UseAuthentication();
app.UseAuthorization();

// Automatic database migration
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AisamContext>();
        if (context.Database.IsNpgsql())
        {
            context.Database.Migrate();
        }

        // Auto-create internal user if not exists on startup
        var userService = services.GetRequiredService<IUserService>();
        await userService.CreateUserInternalAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database or seeding the internal user.");
    }
}

// Note: UserProvisioningMiddleware is no longer needed as users are created during registration
// app.UseUserProvisioning();

app.MapControllers();

app.Run();