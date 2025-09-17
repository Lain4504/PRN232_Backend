# 📱 BookStore Social Media Management API

## 🎯 Tổng quan hệ thống

Hệ thống quản lý đăng bài lên các mạng xã hội với kiến trúc multi-provider. Hiện tại hỗ trợ Facebook Pages, có thể mở rộng cho Instagram, TikTok trong tương lai.

**Luồng chính**: Admin cấu hình Facebook App → User đăng ký tài khoản → Liên kết Facebook → Quản lý Pages → Tạo và đăng bài tự động

### 🚀 Tính năng chính

- 🔐 **Xác thực**: Email/password + JWT token
- 🔗 **Facebook Integration**: OAuth 2.0 + Pages API
- 📱 **Multi-Platform**: Quản lý nhiều Facebook Pages
- ⏰ **Lên lịch**: Đăng bài tự động theo thời gian
- 🏗️ **Mở rộng**: Provider pattern cho các platform khác
- 📊 **Theo dõi**: Trạng thái đăng bài và lỗi

---

## 🏗️ Kiến trúc hệ thống

### Cấu trúc dự án
```
BookStore.API/           # Web API controllers
BookStore.Data/          # Entity models
BookStore.Repositories/  # Data access layer
BookStore.Services/      # Business logic
BookStore.Common/        # Shared models & DTOs
```

### Database Schema
```
User (1) -----> (N) SocialAccount (1) -----> (N) SocialTarget
                       |                           |
                       |                           |
                       +--------> (N) Post <-------+
```

---

## 🛠️ Hướng dẫn cài đặt từ Admin

### Bước 1: Cấu hình Facebook App

#### 1.1 Tạo Facebook App
1. Truy cập [Facebook Developers](https://developers.facebook.com/apps/)
2. Click **"Create App"** → chọn **"Business"**
3. Nhập thông tin:
   - App Name: `BookStore Social Manager`
   - App Contact Email: email của bạn
   - Business Account: (tùy chọn)

#### 1.2 Cấu hình Facebook Login
1. Trong dashboard app, click **"Add Product"**
2. Tìm **"Facebook Login"** → click **"Set up"**
3. Chọn **"Web"** platform
4. Nhập **Site URL**: `http://localhost:5283`
5. Vào **Facebook Login Settings**:
   - **Valid OAuth Redirect URIs**: 
     ```
     http://localhost:5283/auth/facebook/callback
     https://localhost:7079/auth/facebook/callback
     ```
   - **Deauthorize Callback URL**: `http://localhost:5283/auth/facebook/deauth`

#### 1.3 Thêm Pages API
1. Click **"Add Product"** → tìm **"Pages API"**
2. Click **"Set up"** để kích hoạt

#### 1.4 Cấu hình App Domains
1. Vào **App Settings → Basic**
2. Thêm **App Domains**: `localhost`
3. **Privacy Policy URL**: `http://localhost:5283/privacy`
4. **Terms of Service URL**: `http://localhost:5283/terms`

#### 1.5 Request Permissions (Quan trọng!)
1. Vào **App Review → Permissions and Features**
2. Request các permissions sau:
   - `pages_manage_posts` - Đăng bài lên pages
   - `pages_read_engagement` - Đọc thống kê
   - `pages_show_list` - Liệt kê pages của user

**Lưu ý**: Để test, bạn có thể thêm Test Users hoặc chuyển app sang Development mode.

### Bước 2: Cấu hình Database

#### 2.1 Cài đặt PostgreSQL
```bash
# Sử dụng Docker (khuyến nghị)
docker run --name postgres-bookstore \
  -e POSTGRES_DB=BookStoreMultiProvider \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=root \
  -p 5432:5432 \
  -d postgres:15

# Hoặc cài đặt trực tiếp PostgreSQL trên Windows
```

#### 2.2 Cấu hình Connection String
Cập nhật `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=BookStoreMultiProvider;Username=postgres;Password=root"
  }
}
```

### Bước 3: Cấu hình ứng dụng

#### 3.1 Cập nhật Facebook Settings
Mở `BookStore.API/appsettings.json` và thay đổi:
```json
{
  "FacebookSettings": {
    "AppId": "YOUR_FACEBOOK_APP_ID",
    "AppSecret": "YOUR_FACEBOOK_APP_SECRET", 
    "RedirectUri": "http://localhost:5283/auth/facebook/callback",
    "GraphApiVersion": "v20.0",
    "RequiredPermissions": [
      "pages_manage_posts",
      "pages_read_engagement", 
      "pages_show_list"
    ]
  }
}
```

**Cách lấy App ID và App Secret**:
1. Vào Facebook App Dashboard
2. **App Settings → Basic**
3. Copy **App ID** và **App Secret**

#### 3.2 Cấu hình JWT (tùy chọn)
```json
{
  "JwtSettings": {
    "SecretKey": "BookStore_Social_Media_Secret_Key_2025_Very_Long_Secret_For_JWT_Tokens",
    "Issuer": "BookStore.API",
    "Audience": "BookStore.Client", 
    "ExpirationHours": 24
  }
}
```

### Bước 4: Chạy ứng dụng

#### 4.1 Khởi tạo Database
```bash
cd BookStore.Repositories
dotnet ef database update --startup-project ..\BookStore.API
```

#### 4.2 Chạy API
```bash
cd BookStore.API
dotnet run
```

API sẽ chạy tại:
- HTTP: `http://localhost:5283`
- HTTPS: `https://localhost:7079`  
- Swagger UI: `http://localhost:5283/swagger`

---

## 🔄 Luồng sử dụng từ phía User

### Bước 1: Đăng ký tài khoản

#### 1.1 Tạo tài khoản mới
```bash
POST /api/user/register
Content-Type: application/json

{
  "email": "user@example.com",
  "username": "myusername",
  "password": "MyPassword123!"
}
```

**Response thành công**:
```json
{
  "success": true,
  "message": "Đăng ký thành công",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-09-19T10:30:00Z",
    "user": {
      "id": 1,
      "email": "user@example.com", 
      "username": "myusername",
      "socialAccounts": []
    }
  }
}
```

#### 1.2 Đăng nhập (nếu đã có tài khoản)
```bash
POST /api/user/login
Content-Type: application/json

{
  "emailOrUsername": "user@example.com",
  "password": "MyPassword123!"
}
```

### Bước 2: Liên kết tài khoản Facebook

#### 2.1 Lấy URL xác thực
```bash
GET /auth/facebook?state=optional_state_parameter

# Response:
{
  "success": true,
  "data": {
    "authUrl": "https://www.facebook.com/v20.0/dialog/oauth?client_id=123&redirect_uri=...",
    "state": "unique_state_value"
  }
}
```

#### 2.2 User thực hiện OAuth Flow
1. User click vào `authUrl` từ response trên
2. Facebook sẽ redirect đến trang đăng nhập
3. User đăng nhập và cho phép ứng dụng truy cập
4. Facebook redirect về `callback URL` với `code` parameter

#### 2.3 Xử lý callback (tự động)
```bash
GET /auth/facebook/callback?code=AUTHORIZATION_CODE&state=STATE_VALUE&userId=1
```

**Lưu ý**: Tham số `userId` cần thiết để liên kết account. Trong production, thông tin này nên lấy từ session hoặc token.

### Bước 3: Quản lý Facebook Pages

#### 3.1 Xem danh sách Social Accounts
```bash
GET /api/social/accounts/user/1
Authorization: Bearer YOUR_JWT_TOKEN

# Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "provider": "facebook",
      "providerUserId": "facebook_user_id",
      "createdAt": "2025-09-18T12:00:00Z",
      "targets": []
    }
  ]
}
```

#### 3.2 Đồng bộ Facebook Pages
```bash
POST /api/social/targets/sync/1
Authorization: Bearer YOUR_JWT_TOKEN

# Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "providerTargetId": "page_id_123",
      "name": "My Business Page",
      "type": "page",
      "category": "Business"
    }
  ]
}
```

#### 3.3 Xem danh sách Pages
```bash
GET /api/social/targets/account/1
Authorization: Bearer YOUR_JWT_TOKEN
```

### Bước 4: Tạo và đăng bài

#### 4.1 Đăng bài ngay lập tức
```bash
POST /api/posts
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "userId": 1,
  "socialAccountId": 1,
  "socialTargetId": 1,
  "message": "Hello from BookStore Social Manager! 🚀",
  "linkUrl": "https://example.com",
  "imageUrl": "https://example.com/image.jpg",
  "publishImmediately": true
}
```

#### 4.2 Lên lịch đăng bài
```bash
POST /api/posts/schedule  
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "userId": 1,
  "socialAccountId": 1,
  "socialTargetId": 1,
  "message": "Scheduled post for tomorrow! ⏰",
  "scheduledTime": "2025-09-19T09:00:00Z"
}
```

#### 4.3 Đăng bài draft
```bash
POST /api/posts/{postId}/publish
Authorization: Bearer YOUR_JWT_TOKEN
```

#### 4.4 Xem danh sách bài đăng
```bash
GET /api/posts/user/1
Authorization: Bearer YOUR_JWT_TOKEN

# Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "message": "Hello from BookStore!",
      "status": "Posted",
      "providerPostId": "page_post_id_123",
      "scheduledTime": null,
      "postedAt": "2025-09-18T12:30:00Z",
      "socialTarget": {
        "name": "My Business Page"
      }
    }
  ]
}
```

---

## 🔧 Testing và Debugging

### Sử dụng Swagger UI
1. Mở `http://localhost:5283/swagger`
2. Click **"Authorize"** và nhập JWT token
3. Test các API endpoints

### Kiểm tra Database
```sql
-- Xem users
SELECT * FROM "Users";

-- Xem social accounts  
SELECT * FROM "SocialAccounts";

-- Xem pages/targets
SELECT * FROM "SocialTargets";

-- Xem posts
SELECT * FROM "Posts";
```

### Debug OAuth Flow
1. Kiểm tra Facebook App settings
2. Verify callback URL chính xác
3. Check permissions đã được approve
4. Xem logs trong console

### Xử lý lỗi thường gặp

#### Lỗi Facebook OAuth
```json
{
  "success": false,
  "message": "Invalid OAuth code",
  "errorCode": "FACEBOOK_AUTH_ERROR"
}
```
**Giải pháp**: Kiểm tra App ID, Secret và callback URL

#### Lỗi permissions
```json
{
  "success": false, 
  "message": "Missing required permissions",
  "errorCode": "INSUFFICIENT_PERMISSIONS"
}
```
**Giải pháp**: Request đúng permissions trong Facebook App Review

#### Lỗi expired token
```json
{
  "success": false,
  "message": "Facebook access token expired", 
  "errorCode": "TOKEN_EXPIRED"
}
```
**Giải pháp**: User cần liên kết lại Facebook account

---

## � Mở rộng hệ thống

### Thêm Instagram Provider
1. Tạo `InstagramProvider.cs` implement `IProviderService`
2. Cấu hình Instagram Basic Display API
3. Update dependency injection

### Thêm TikTok Provider
1. Tạo `TikTokProvider.cs` 
2. Cấu hình TikTok for Developers
3. Implement posting API

### Monitoring và Analytics
- Thêm logging với Serilog
- Implement metrics collection
- Dashboard cho admin

---

## 📞 Hỗ trợ

Nếu gặp vấn đề:
1. Kiểm tra logs trong console
2. Verify Facebook App configuration
3. Test với Swagger UI
4. Check database connections

**Happy coding! 🎉**

```   {

1. POST /api/user/register        # Đăng ký local account     "ConnectionStrings": {

2. POST /api/user/login           # Login → JWT token       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BookStoreFacebook;Trusted_Connection=true;MultipleActiveResultSets=true"

3. GET /auth/facebook/url         # Lấy Facebook OAuth URL     }

4. [User authorize trên Facebook] # OAuth flow   }

5. GET /api/social/targets        # Xem pages đã liên kết   ```

6. POST /api/posts/publish        # Đăng bài ngay

7. POST /api/posts/schedule       # Lên lịch đăng2. **Run Migrations**

```   ```bash

   cd BookStore.API

### Example Usage:   dotnet ef migrations add InitialFacebookIntegration

   dotnet ef database update

**Đăng ký:**   ```

```bash

curl -X POST http://localhost:5000/api/user/register \## API Endpoints

  -H "Content-Type: application/json" \

  -d '{"email":"user@example.com","username":"myuser","password":"pass123","confirmPassword":"pass123"}'### Authentication

```

#### Get Authorization URL

**Đăng nhập:**```http

```bashGET /auth/facebook?state=optional_state

curl -X POST http://localhost:5000/api/user/login \```

  -H "Content-Type: application/json" \

  -d '{"emailOrUsername":"user@example.com","password":"pass123"}'**Response:**

``````json

{

**Đăng bài:**  "success": true,

```bash  "data": {

curl -X POST http://localhost:5000/api/posts/publish \    "authUrl": "https://www.facebook.com/v20.0/dialog/oauth?client_id=..."

  -H "Authorization: Bearer YOUR_JWT_TOKEN" \  }

  -H "Content-Type: application/json" \}

  -d '{"userId":1,"socialAccountId":1,"socialTargetId":1,"message":"Hello world! 🚀"}'```

```

#### Handle OAuth Callback

---```http

GET /auth/facebook/callback?code=authorization_code&state=optional_state

## 🏗️ Architecture```



```**Response:**

BookStore.API          → Controllers, JWT Auth, Middleware```json

BookStore.Services     → Business Logic, Provider Services  {

BookStore.Repositories → Data Access, EF Core  "success": true,

BookStore.Data         → Models (User, SocialAccount, Post)  "data": {

BookStore.Common       → DTOs, Responses    "user": {

```      "id": 1,

      "facebookId": "facebook_user_id",

### Core Models:      "createdAt": "2024-01-01T00:00:00Z"

- **User**: Local account (email/password)    },

- **SocialAccount**: OAuth tokens per provider    "pages": [

- **SocialTarget**: Pages/profiles per platform      {

- **Post**: Content with scheduling        "id": "page_id",

        "name": "Page Name",

### Provider Pattern:        "category": "Business",

```csharp        "isActive": true

public interface IProviderService {      }

    Task<string> GetAuthUrlAsync(string userId);    ],

    Task<SocialAccount> ExchangeCodeAsync(string code);    "accessToken": "user_access_token"

    Task<List<SocialTarget>> GetTargetsAsync(SocialAccount account);  }

    Task<PostResult> PublishAsync(Post post);}

}```



// FacebookProvider ✅ | InstagramProvider 🔄 | TikTokProvider 🔄### Page Management

```

#### Get User Pages

---```http

GET /api/facebook/pages/user/{userId}

## 📋 API Reference```



| Endpoint | Method | Description | Auth |#### Sync Pages

|----------|--------|-------------|------|```http

| `/api/user/register` | POST | Đăng ký | ❌ |POST /api/facebook/pages/sync

| `/api/user/login` | POST | Đăng nhập | ❌ |Content-Type: application/json

| `/api/user/profile` | GET | Profile | ✅ |

| `/auth/facebook/url` | GET | OAuth URL | ❌ |{

| `/api/social/targets` | GET | Pages list | ✅ |  "userId": 1,

| `/api/posts/publish` | POST | Đăng ngay | ✅ |  "accessToken": "user_access_token"

| `/api/posts/schedule` | POST | Lên lịch | ✅ |}

| `/api/posts` | GET | Bài đăng | ✅ |```



**Auth**: ✅ = Require `Authorization: Bearer {jwt_token}`### Post Management



---#### Create and Publish Post

```http

## 🔧 Tech StackPOST /api/facebook/posts

Content-Type: application/json

- **.NET 8** - API Framework

- **PostgreSQL** - Database  {

- **Entity Framework Core** - ORM  "userId": 1,

- **JWT** - Authentication  "pageId": "facebook_page_id",

- **BCrypt** - Password hashing  "message": "Hello from AISAM! 🚀",

- **Facebook Graph API** - Social integration  "linkUrl": "https://example.com",

  "published": true

---}

```

## 🚀 Production Ready

#### Schedule Post

- ✅ Local user authentication```http

- ✅ Facebook OAuth integrationPOST /api/facebook/posts/schedule

- ✅ Multi-provider architectureContent-Type: application/json

- ✅ Background post scheduling

- ✅ Error handling & logging{

- ✅ Clean architecture pattern  "userId": 1,

  "pageId": "facebook_page_id",

**API Docs**: `http://localhost:5000/swagger` sau khi chạy ứng dụng  "message": "Scheduled post for later!",

  "scheduledTime": "2024-12-25T10:00:00Z"

**Next**: Thêm Instagram & TikTok providers sử dụng cùng pattern}
```

#### Get User Posts
```http
GET /api/facebook/posts/user/{userId}
```

#### Manually Publish Draft Post
```http
POST /api/facebook/posts/{postId}/publish
```

## Testing the Integration

### 1. Start the Application
```bash
cd BookStore.API
dotnet run
```

### 2. Test OAuth Flow

1. **Get authorization URL:**
   ```bash
   curl -X GET "http://localhost:5000/auth/facebook"
   ```

2. **Open the returned URL in browser** and authorize the app

3. **The callback will be processed automatically** and return user/page data

### 3. Test Post Creation

```bash
curl -X POST "http://localhost:5000/api/facebook/posts" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 1,
    "pageId": "your_page_id",
    "message": "Test post from AISAM API! 🎉",
    "published": true
  }'
```

### 4. Test Post Scheduling

```bash
curl -X POST "http://localhost:5000/api/facebook/posts/schedule" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 1,
    "pageId": "your_page_id",
    "message": "This post will be published later!",
    "scheduledTime": "2024-12-25T15:30:00Z"
  }'
```

## Architecture

### Database Schema

```sql
-- Users table
CREATE TABLE Users (
    Id int IDENTITY(1,1) PRIMARY KEY,
    FacebookId nvarchar(255) UNIQUE,
    AccessToken nvarchar(max),
    RefreshToken nvarchar(max),
    TokenExpiresAt datetime2,
    CreatedAt datetime2 DEFAULT GETUTCDATE(),
    UpdatedAt datetime2 DEFAULT GETUTCDATE()
);

-- Facebook Pages table
CREATE TABLE FacebookPages (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId int FOREIGN KEY REFERENCES Users(Id),
    PageId nvarchar(255) UNIQUE NOT NULL,
    PageName nvarchar(255) NOT NULL,
    PageAccessToken nvarchar(max) NOT NULL,
    Category nvarchar(255),
    ProfilePictureUrl nvarchar(max),
    IsActive bit DEFAULT 1,
    CreatedAt datetime2 DEFAULT GETUTCDATE(),
    UpdatedAt datetime2 DEFAULT GETUTCDATE()
);

-- Posts table
CREATE TABLE Posts (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId int FOREIGN KEY REFERENCES Users(Id),
    FacebookPageId int FOREIGN KEY REFERENCES FacebookPages(Id),
    FacebookPostId nvarchar(255),
    Message nvarchar(max) NOT NULL,
    LinkUrl nvarchar(max),
    ImageUrl nvarchar(max),
    ScheduledTime datetime2,
    PostedAt datetime2,
    Status int DEFAULT 0, -- 0=Draft, 1=Scheduled, 2=Posted, 3=Failed, 4=Cancelled
    ErrorMessage nvarchar(max),
    CreatedAt datetime2 DEFAULT GETUTCDATE(),
    UpdatedAt datetime2 DEFAULT GETUTCDATE()
);
```

### Service Layer

- **FacebookAuthService**: Handles OAuth flow and token management
- **FacebookPostService**: Manages post creation and publishing
- **FacebookPageService**: Handles page synchronization
- **ScheduledPostProcessorService**: Background service for scheduled posts

### Error Handling

The integration includes comprehensive error handling for:
- Invalid or expired tokens
- Insufficient permissions
- Network timeouts
- Facebook API rate limits
- Content policy violations

## Security Considerations

- ✅ Access tokens are stored encrypted in database
- ✅ Comprehensive input validation
- ✅ HTTPS enforcement for production
- ✅ Rate limiting implementation
- ✅ Audit logging for all API calls

## Monitoring and Logging

- All Facebook API calls are logged with correlation IDs
- Performance metrics for post processing
- Error tracking and alerting
- Token expiration monitoring

## Future Enhancements

### Phase 2 Features:
- Video posting support
- Instagram integration
- Post analytics and insights
- Bulk posting capabilities
- Template management

### Phase 3 Features:
- AI content generation
- Optimal posting time recommendations
- A/B testing framework
- Advanced scheduling rules

## Support

For technical support or questions about this integration, please contact the development team or refer to the Facebook Graph API documentation at https://developers.facebook.com/docs/graph-api/

---

**Note:** This integration is designed for the AISAM BookStore project and follows Facebook's API guidelines and best practices for production use.