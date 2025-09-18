# Environment Variables Setup Guide

## Tổng Quan
Dự án BookStore đã được cấu hình để sử dụng file `.env` để quản lý các biến môi trường một cách an toàn. Điều này giúp:
- Bảo mật thông tin nhạy cảm (database credentials, API keys)
- Dễ dàng chuyển đổi giữa các môi trường (development, staging, production)
- Tránh commit sensitive data vào source control

## Cấu Hình Đã Hoàn Thành

### 1. Package Đã Cài Đặt
```bash
DotNetEnv (3.1.1) - Load biến môi trường từ file .env
```

### 2. File .env Đã Tạo
**Vị trí**: `BookStore.API/.env`
```env
# Database Configuration
DB_HOST=localhost
DB_PORT=5432
DB_NAME=BookStoreMultiProvider
DB_USER=postgres
DB_PASSWORD=root

# Complete Connection String (alternative to separate variables)
CONNECTION_STRING=Host=localhost;Port=5432;Database=BookStoreMultiProvider;Username=postgres;Password=root

# JWT Configuration
JWT_SECRET_KEY=BookStore_Social_Media_Secret_Key_2025_Very_Long_Secret_For_JWT_Tokens

# Facebook Configuration
FACEBOOK_APP_ID=1987104132027477
FACEBOOK_APP_SECRET=dfd6660844b0d4b99376322f19a58aad

# Development Environment
ASPNETCORE_ENVIRONMENT=Development
```

### 3. .gitignore Đã Cập Nhật
File `.gitignore` đã được cập nhật để loại trừ file `.env`:
```gitignore
# Environment variables
.env
.env.local
.env.development
.env.production
```

### 4. Program.cs Đã Cấu Hình
Biến môi trường được load và inject vào Configuration:
```csharp
// Load environment variables from .env file
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Load environment variables into configuration
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
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
```

### 5. appsettings.json Đã Làm Sạch
Các giá trị sensitive đã được loại bỏ và để trống:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "JwtSettings": {
    "SecretKey": "",
    "Issuer": "BookStore.API",
    "Audience": "BookStore.Client",
    "ExpirationHours": 24
  },
  "FacebookSettings": {
    "AppId": "",
    "AppSecret": "",
    "RedirectUri": "http://localhost:5000/auth/facebook/callback",
    "GraphApiVersion": "v20.0",
    "BaseUrl": "https://graph.facebook.com",
    "OAuthUrl": "https://www.facebook.com",
    "RequiredPermissions": [
      "pages_manage_posts",
      "pages_read_engagement",
      "pages_show_list"
    ]
  }
}
```

## Cách Sử Dụng

### 1. Development Environment
```bash
# Navigate to API project
cd BookStore.API

# Run with Development environment
dotnet run --environment Development
```

### 2. Production Environment
Tạo file `.env.production` với cấu hình production:
```env
# Production Database
CONNECTION_STRING=Host=production-server;Port=5432;Database=BookStoreProduction;Username=produser;Password=secure-password

# Production JWT Secret
JWT_SECRET_KEY=production-jwt-secret-very-long-and-secure

# Production Facebook App
FACEBOOK_APP_ID=production-facebook-app-id
FACEBOOK_APP_SECRET=production-facebook-app-secret

ASPNETCORE_ENVIRONMENT=Production
```

### 3. Docker Environment
Trong `docker-compose.yml`:
```yaml
services:
  api:
    build: .
    environment:
      - CONNECTION_STRING=Host=postgres;Port=5432;Database=BookStore;Username=postgres;Password=postgres
      - JWT_SECRET_KEY=docker-jwt-secret
      - FACEBOOK_APP_ID=your-facebook-app-id
      - FACEBOOK_APP_SECRET=your-facebook-app-secret
```

## Bảo Mật

### Các Điều Cần Lưu Ý:
1. **KHÔNG BAO GIỜ** commit file `.env` vào Git
2. Sử dụng `.env.example` để document structure:
   ```env
   # Database Configuration
   DB_HOST=your-database-host
   DB_PORT=your-database-port
   DB_NAME=your-database-name
   DB_USER=your-database-user
   DB_PASSWORD=your-database-password
   
   # JWT Configuration
   JWT_SECRET_KEY=your-jwt-secret-key
   
   # Facebook Configuration
   FACEBOOK_APP_ID=your-facebook-app-id
   FACEBOOK_APP_SECRET=your-facebook-app-secret
   ```

3. Trong production, sử dụng system environment variables thay vì file `.env`
4. Rotate secrets định kỳ

## Kiểm Tra Cấu Hình

### 1. Verify Environment Variables
Thêm log để kiểm tra:
```csharp
// Debug environment variables (remove in production)
Console.WriteLine($"DB_HOST: {Environment.GetEnvironmentVariable("DB_HOST")}");
Console.WriteLine($"CONNECTION_STRING: {Environment.GetEnvironmentVariable("CONNECTION_STRING")}");
```

### 2. Test Database Connection
```bash
dotnet run --environment Development
# Kiểm tra log để đảm bảo database connection thành công
```

### 3. Test API Endpoints
```bash
# Test health endpoint
curl http://localhost:5283/health

# Test user endpoints
curl http://localhost:5283/api/users
```

## Troubleshooting

### Lỗi "Host can't be null"
- Kiểm tra file `.env` có tồn tại không
- Kiểm tra biến `CONNECTION_STRING` có đúng format không
- Đảm bảo `DotNetEnv.Env.Load()` được gọi trước `WebApplication.CreateBuilder()`

### Environment Variables Không Load
- Đảm bảo file `.env` nằm trong thư mục `BookStore.API`
- Kiểm tra file `.env` có newline ở cuối file không
- Verify package `DotNetEnv` đã được install

### Production Issues
- Sử dụng system environment variables thay vì file `.env`
- Kiểm tra application có quyền đọc environment variables không
- Verify connection string format đúng với production database

## Kết Luận
Cấu hình environment variables đã hoàn thành và tested thành công. Ứng dụng có thể:
- ✅ Load biến môi trường từ file `.env`
- ✅ Connect database với thông tin từ environment variables
- ✅ Sử dụng Facebook credentials từ environment variables
- ✅ Bảo mật thông tin sensitive không commit vào Git
- ✅ Dễ dàng chuyển đổi giữa các môi trường khác nhau