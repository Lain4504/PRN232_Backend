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

## � Hướng dẫn chi tiết: Facebook Authentication cho User

### Tình huống: User đã có tài khoản, muốn kết nối Facebook để đăng bài

Sau khi user đã đăng nhập vào hệ thống, họ cần thực hiện Facebook OAuth để có quyền đăng bài lên các Facebook Pages.

### Bước 1: Khởi tạo Facebook Auth Flow

#### 1.1 Frontend gọi API lấy Auth URL
```javascript
// JavaScript Example (có thể dùng trong React, Vue, etc.)
const initFacebookAuth = async () => {
  try {
    // Gọi API để lấy Facebook auth URL
    const response = await fetch('/auth/facebook?state=user_123', {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${userJwtToken}`,
        'Content-Type': 'application/json'
      }
    });
    
    const result = await response.json();
    
    if (result.success) {
      // Chuyển hướng user đến Facebook OAuth
      window.location.href = result.data.authUrl;
    }
  } catch (error) {
    console.error('Error starting Facebook auth:', error);
  }
};
```

#### 1.2 API Response - Auth URL
```json
{
  "success": true,
  "data": {
    "authUrl": "https://www.facebook.com/v20.0/dialog/oauth?client_id=1987104132027477&redirect_uri=http%3A%2F%2Flocalhost%3A5283%2Fauth%2Ffacebook%2Fcallback&scope=pages_manage_posts%2Cpages_read_engagement%2Cpages_show_list&response_type=code&state=user_123",
    "state": "user_123"
  }
}
```

### Bước 2: User thực hiện OAuth trên Facebook

#### 2.1 Trang Facebook Login
User sẽ được chuyển đến trang Facebook với:
- **Login Form**: Nếu chưa đăng nhập Facebook
- **Permission Dialog**: Xác nhận cấp quyền cho app

#### 2.2 Facebook hiển thị các quyền yêu cầu:
```
BookStore Social Manager muốn:
✅ Quản lý bài đăng trên các Trang bạn quản lý
✅ Xem thông tin tương tác của Trang
✅ Xem danh sách các Trang bạn quản lý

[ Tiếp tục ]  [ Hủy ]
```

#### 2.3 User actions:
- **"Tiếp tục"**: Cấp quyền và chuyển về callback
- **"Hủy"**: Hủy và quay về ứng dụng với error

### Bước 3: Xử lý Callback và lưu thông tin

#### 3.1 Facebook Callback (Tự động)
```
GET /auth/facebook/callback?code=AQB...XYZ&state=user_123&userId=1
```

**Quan trọng**: `userId` cần được gửi kèm để hệ thống biết liên kết với user nào.

#### 3.2 Backend xử lý:
1. **Verify state** để tránh CSRF attack
2. **Exchange code** cho access token
3. **Lấy user info** từ Facebook
4. **Lấy danh sách Pages** user quản lý
5. **Lưu vào database**

#### 3.3 Response thành công:
```json
{
  "success": true,
  "data": {
    "user": {
      "id": 1,
      "email": "user@example.com",
      "socialAccounts": [
        {
          "id": 1,
          "provider": "facebook",
          "providerUserId": "fb_user_123456",
          "createdAt": "2025-09-18T12:00:00Z",
          "targets": [
            {
              "id": 1,
              "providerTargetId": "page_789",
              "name": "My Business Page",
              "type": "page",
              "category": "Business"
            }
          ]
        }
      ]
    },
    "message": "Facebook account linked successfully"
  }
}
```

### Bước 4: Frontend xử lý kết quả

#### 4.1 Success Handler (Redirect từ callback)
```javascript
// Callback page handler
const handleFacebookCallback = () => {
  const urlParams = new URLSearchParams(window.location.search);
  const success = urlParams.get('success');
  const error = urlParams.get('error');
  
  if (success === 'true') {
    // Hiển thị thông báo thành công
    showNotification('✅ Kết nối Facebook thành công!');
    
    // Redirect về dashboard hoặc reload data
    window.location.href = '/dashboard';
  } else if (error) {
    // Hiển thị lỗi
    showNotification(`❌ Lỗi: ${error}`);
  }
};
```

#### 4.2 Update UI sau khi kết nối
```javascript
// Reload user data to show connected Facebook account
const refreshUserData = async () => {
  const response = await fetch('/api/user/profile', {
    headers: {
      'Authorization': `Bearer ${userJwtToken}`
    }
  });
  
  const userData = await response.json();
  
  if (userData.success) {
    // Update UI để hiển thị Facebook account đã kết nối
    updateSocialAccountsUI(userData.data.socialAccounts);
  }
};
```

### Bước 5: Kiểm tra và sử dụng

#### 5.1 Xem Facebook Pages đã kết nối
```bash
GET /api/social/targets/account/1
Authorization: Bearer YOUR_JWT_TOKEN

# Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "providerTargetId": "page_789",
      "name": "My Business Page", 
      "type": "page",
      "category": "Business",
      "isActive": true
    },
    {
      "id": 2,
      "providerTargetId": "page_456",
      "name": "My Personal Page",
      "type": "page", 
      "category": "Personal Blog",
      "isActive": true
    }
  ]
}
```

#### 5.2 Test đăng bài
```bash
POST /api/posts
Authorization: Bearer YOUR_JWT_TOKEN

{
  "userId": 1,
  "socialAccountId": 1,
  "socialTargetId": 1,
  "message": "🎉 Test post từ BookStore Social Manager!",
  "publishImmediately": true
}
```

### ⚠️ Các lỗi thường gặp và cách xử lý

#### Lỗi 1: "Invalid OAuth 2.0 User"
**Nguyên nhân**: User từ chối cấp quyền hoặc hủy OAuth flow
**Giải pháp**: Yêu cầu user thử lại và đảm bảo click "Tiếp tục"

#### Lỗi 2: "Permissions not granted"
**Nguyên nhân**: User không cấp đủ quyền required
**Giải pháp**: 
- Kiểm tra Facebook App có yêu cầu đúng permissions
- Yêu cầu user revoke và thử lại

#### Lỗi 3: "No pages found"
**Nguyên nhân**: User không quản lý page nào
**Giải pháp**: 
- Hướng dẫn user tạo Facebook Page
- Hoặc được admin thêm vào page có sẵn

#### Lỗi 4: "Token expired"
**Nguyên nhân**: Facebook access token hết hạn
**Giải pháp**: User cần thực hiện lại OAuth flow

### 🔄 Refresh Token khi hết hạn

```javascript
// Kiểm tra token status
const checkFacebookTokenStatus = async (socialAccountId) => {
  try {
    const response = await fetch(`/api/social/accounts/${socialAccountId}/status`, {
      headers: {
        'Authorization': `Bearer ${userJwtToken}`
      }
    });
    
    const result = await response.json();
    
    if (!result.data.isValid) {
      // Token hết hạn, cần reconnect
      showReconnectDialog();
    }
  } catch (error) {
    console.error('Error checking token status:', error);
  }
};
```

---

## 🧪 Testing với Facebook Graph API Explorer (Không cần Frontend)

### Tình huống: Test API với Swagger + Facebook Graph API Explorer

Nếu bạn chưa có frontend và chỉ muốn test qua Swagger, bạn có thể sử dụng Facebook Graph API Explorer để generate access token và test trực tiếp.

### Bước 1: Sử dụng Facebook Graph API Explorer

#### 1.1 Truy cập Graph API Explorer
Vào: https://developers.facebook.com/tools/explorer/1987104132027477/

#### 1.2 Generate User Access Token
1. **Chọn Application**: `BookStore Social Manager` (App ID: 1987104132027477)
2. **Click "Generate Access Token"**
3. **Select Permissions** (quan trọng!):
   ```
   ✅ pages_manage_posts
   ✅ pages_read_engagement  
   ✅ pages_show_list
   ✅ pages_read_user_content (nếu có)
   ```
4. **Click "Generate Access Token"** → Login Facebook và cấp quyền
5. **Copy Access Token** (dạng: `EAAcX...`)

#### 1.3 Test User Token trên Graph Explorer
```bash
# Test API call
GET /me?fields=id,name,email

# Expected Response:
{
  "id": "your_facebook_user_id",
  "name": "Your Name", 
  "email": "your@email.com"
}
```

#### 1.4 Lấy danh sách Pages
```bash
# Get user's pages
GET /me/accounts?fields=id,name,category,access_token

# Response:
{
  "data": [
    {
      "id": "page_id_123",
      "name": "My Business Page",
      "category": "Business",
      "access_token": "EAAcX...page_token"
    }
  ]
}
```

### Bước 2: Manual Testing qua Swagger (Bypass OAuth)

Vì chưa có frontend, bạn có thể tạo data trực tiếp trong database để test.

#### 2.1 Tạo User trong Database
```sql
-- Connect vào PostgreSQL
INSERT INTO "Users" ("Email", "Username", "PasswordHash", "CreatedAt", "UpdatedAt")
VALUES ('test@example.com', 'testuser', 'hashed_password', NOW(), NOW());

-- Lấy User ID vừa tạo
SELECT * FROM "Users" WHERE "Email" = 'test@example.com';
```

#### 2.2 Manual Insert SocialAccount
```sql
-- Thay YOUR_USER_ID và access_token từ Graph Explorer
INSERT INTO "SocialAccounts" (
    "UserId", 
    "Provider", 
    "ProviderUserId", 
    "AccessToken", 
    "IsActive", 
    "CreatedAt", 
    "UpdatedAt"
) VALUES (
    1, -- YOUR_USER_ID
    'facebook',
    'your_facebook_user_id', -- từ Graph Explorer
    'EAAcX...your_access_token', -- từ Graph Explorer  
    true,
    NOW(),
    NOW()
);
```

#### 2.3 Manual Insert SocialTarget (Facebook Page)
```sql
-- Thay page info từ Graph Explorer
INSERT INTO "SocialTargets" (
    "SocialAccountId",
    "ProviderTargetId", 
    "Name",
    "Type",
    "AccessToken", -- Page access token
    "IsActive",
    "CreatedAt",
    "UpdatedAt"
) VALUES (
    1, -- SocialAccount ID vừa tạo
    'page_id_123', -- từ Graph Explorer
    'My Business Page', -- từ Graph Explorer
    'page',
    'EAAcX...page_access_token', -- từ Graph Explorer
    true,
    NOW(),
    NOW()
);
```

### Bước 3: Test API qua Swagger

#### 3.1 Generate JWT Token
```bash
# Login để lấy JWT
POST /api/user/login
{
  "emailOrUsername": "test@example.com",
  "password": "your_password"
}

# Copy JWT token từ response
```

### 🔑 Bước quan trọng: Liên kết Page Access Token với User

Nếu bạn đã có Page Access Token từ Graph Explorer, đây là cách để liên kết nó với user đang đăng nhập:

#### 3.1.1 Manual Link - Insert SocialAccount và SocialTarget
```sql
-- 1. Insert SocialAccount (Facebook User Account)
INSERT INTO "SocialAccounts" (
    "UserId", 
    "Provider", 
    "ProviderUserId", 
    "AccessToken", 
    "IsActive", 
    "CreatedAt", 
    "UpdatedAt"
) VALUES (
    1, -- ID của user đang đăng nhập (lấy từ JWT hoặc database)
    'facebook',
    'YOUR_FACEBOOK_USER_ID', -- Lấy từ Graph Explorer: GET /me
    'YOUR_USER_ACCESS_TOKEN', -- User token từ Graph Explorer  
    true,
    NOW(),
    NOW()
);

-- 2. Insert SocialTarget (Facebook Page)
INSERT INTO "SocialTargets" (
    "SocialAccountId",
    "ProviderTargetId", 
    "Name",
    "Type",
    "AccessToken", -- PAGE ACCESS TOKEN quan trọng!
    "Category",
    "IsActive",
    "CreatedAt",
    "UpdatedAt"
) VALUES (
    1, -- SocialAccount ID vừa tạo ở trên
    'YOUR_PAGE_ID', -- Page ID từ Graph Explorer
    'YOUR_PAGE_NAME', -- Page name từ Graph Explorer
    'page',
    'YOUR_PAGE_ACCESS_TOKEN', -- ⭐ PAGE TOKEN này để đăng bài
    'Business', -- Category từ Graph Explorer
    true,
    NOW(),
    NOW()
);
```

#### 3.1.2 Lấy thông tin cần thiết từ Graph Explorer
```bash
# 1. Get User Info
GET /me?fields=id,name,email
Response: { "id": "facebook_user_id_123", "name": "Your Name" }

# 2. Get User's Pages với Page Access Tokens
GET /me/accounts?fields=id,name,category,access_token
Response: {
  "data": [
    {
      "id": "page_id_456", 
      "name": "My Business Page",
      "category": "Business",
      "access_token": "EAAcX...page_token" // ⭐ Đây là token để đăng bài
    }
  ]
}
```

#### 3.1.3 Alternative - API Mock để Link (Nếu có API endpoint)
```bash
# Nếu có API endpoint để link manual (cần implement)
POST /api/social/link-manual
Authorization: Bearer YOUR_JWT_TOKEN
{
  "provider": "facebook",
  "userAccessToken": "EAAcX...user_token",
  "pageAccessToken": "EAAcX...page_token", 
  "pageId": "page_id_456",
  "pageName": "My Business Page"
}
```

#### 3.2 Verify Link thành công
```bash
GET /api/social/accounts/user/1
Authorization: Bearer YOUR_JWT_TOKEN

# Expected Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "provider": "facebook",
      "providerUserId": "facebook_user_id_123",
      "isActive": true,
      "targets": [
        {
          "id": 1,
          "providerTargetId": "page_id_456",
          "name": "My Business Page",
          "type": "page",
          "isActive": true
        }
      ]
    }
  ]
}
```

#### 3.3 Test Get User Pages
```bash
GET /api/social/targets/account/1  
Authorization: Bearer YOUR_JWT_TOKEN
```

#### 3.4 Test Create Post (Sử dụng Page Token)
```bash
POST /api/posts
Authorization: Bearer YOUR_JWT_TOKEN

{
  "userId": 1,
  "socialAccountId": 1, 
  "socialTargetId": 1,
  "message": "🧪 Test post từ Swagger với Page Access Token!",
  "publishImmediately": true
}

# System sẽ tự động sử dụng Page Access Token đã lưu trong SocialTarget
```

---

## 📱 Quick Setup: Từ Page Access Token đến Post thành công

### Scenario: Bạn có Page Access Token và muốn đăng bài ngay

#### Step 1: Lấy thông tin từ Graph Explorer
```bash
# Trên Graph API Explorer
GET /me?fields=id,name,email
# Note: User ID, Name

GET /me/accounts?fields=id,name,category,access_token  
# Note: Page ID, Page Name, Page Access Token
```

#### Step 2: Tạo hoặc Login User
```bash
# Option A: Tạo user mới
POST /api/user/register
{
  "email": "testuser@example.com",
  "username": "testuser",
  "password": "Password123!"
}

# Option B: Login user có sẵn  
POST /api/user/login
{
  "emailOrUsername": "testuser@example.com", 
  "password": "Password123!"
}

# Lưu JWT token từ response
```

#### Step 3: Link Page Token với User (API Method - Khuyến nghị!)

**🚀 NEW: Sử dụng API thay vì manual SQL**

```bash
POST /auth/link-page-token
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "userId": 1,
  "pageAccessToken": "EAAcPQrvr1FUBPUtw38VYuZA7ConbAI6dFFlqDtsqeZA4NxIZCZBUkZCdf50pEwZBixj34dwGbQTVZCpBY3UFaZBexQjyBnb8X0XFaGWykp60p4UnS0nZCWpFsBxEUdK3NZAqmm0b4WuM7X5QvbogPxmHLxZBCvZCfc57P3euTi9hB5WpDLSPHYO8xd5KxrYhj2sxrmSoCKZBCB0ygytgZCZBxuWT6wP0w2zwZBHfFRNSTlOJyKgcgAZDZD",
  "userAccessToken": "USER_ACCESS_TOKEN_FROM_GRAPH_EXPLORER"
}

# Expected Response:
{
  "success": true,
  "message": "Facebook page linked successfully",
  "data": {
    "id": 1,
    "provider": "facebook",
    "providerUserId": "facebook_user_123",
    "isActive": true,
    "targets": [
      {
        "id": 1,
        "providerTargetId": "page_456",
        "name": "My Business Page",
        "type": "page",
        "isActive": true
      }
    ]
  }
}
```

**⚠️ Lưu ý về Access Token:**
- `pageAccessToken`: **BẮT BUỘC** - Token để đăng bài lên page
- `userAccessToken`: Tùy chọn - Token để lấy thông tin user Facebook

#### Alternative: Step 3 - Link Page Token với User (Manual Database)
```sql
-- Lấy User ID từ JWT hoặc database
SELECT "Id" FROM "Users" WHERE "Email" = 'testuser@example.com';

-- Insert SocialAccount
INSERT INTO "SocialAccounts" (
    "UserId", "Provider", "ProviderUserId", "AccessToken", 
    "IsActive", "CreatedAt", "UpdatedAt"
) VALUES (
    1, -- Thay bằng User ID thật
    'facebook', 
    'FB_USER_ID_FROM_GRAPH_EXPLORER',
    'USER_ACCESS_TOKEN_FROM_GRAPH_EXPLORER',
    true, NOW(), NOW()
);

-- Insert SocialTarget (Facebook Page)  
INSERT INTO "SocialTargets" (
    "SocialAccountId", "ProviderTargetId", "Name", "Type",
    "AccessToken", "Category", "IsActive", "CreatedAt", "UpdatedAt"
) VALUES (
    1, -- SocialAccount ID vừa tạo
    'PAGE_ID_FROM_GRAPH_EXPLORER',
    'PAGE_NAME_FROM_GRAPH_EXPLORER', 
    'page',
    'PAGE_ACCESS_TOKEN_FROM_GRAPH_EXPLORER', -- ⭐ Token để đăng bài
    'Business',
    true, NOW(), NOW()
);
```

#### Step 4: Test Post ngay lập tức
```bash
POST /api/posts
Authorization: Bearer YOUR_JWT_TOKEN

{
  "userId": 1,
  "socialAccountId": 1,
  "socialTargetId": 1, 
  "message": "🎉 Hello World from BookStore API!",
  "publishImmediately": true
}

# Expected Response:
{
  "success": true,
  "data": {
    "id": 1,
    "message": "🎉 Hello World from BookStore API!",
    "status": "Posted",
    "providerPostId": "page_post_id_123", // Facebook Post ID
    "postedAt": "2025-09-18T15:30:00Z"
  }
}
```

#### Step 5: Verify trên Facebook Page
1. Vào Facebook Page của bạn
2. Check tab "Posts" 
3. Xem post vừa được tạo

### 🔥 One-liner Commands (PostgreSQL)

```sql
-- All-in-one setup (thay các giá trị YOUR_* bằng thông tin thật)
WITH user_insert AS (
  INSERT INTO "Users" ("Email", "Username", "PasswordHash", "CreatedAt", "UpdatedAt")
  VALUES ('quick@test.com', 'quickuser', 'hashedpass', NOW(), NOW())
  RETURNING "Id" as user_id
),
social_insert AS (
  INSERT INTO "SocialAccounts" ("UserId", "Provider", "ProviderUserId", "AccessToken", "IsActive", "CreatedAt", "UpdatedAt")
  SELECT user_id, 'facebook', 'YOUR_FB_USER_ID', 'YOUR_USER_TOKEN', true, NOW(), NOW()
  FROM user_insert
  RETURNING "Id" as social_id
)
INSERT INTO "SocialTargets" ("SocialAccountId", "ProviderTargetId", "Name", "Type", "AccessToken", "IsActive", "CreatedAt", "UpdatedAt")
SELECT social_id, 'YOUR_PAGE_ID', 'YOUR_PAGE_NAME', 'page', 'YOUR_PAGE_TOKEN', true, NOW(), NOW()
FROM social_insert;
```

### ⚡ Troubleshooting nhanh

#### Kiểm tra Page Access Token có hoạt động không
```bash
# Test trực tiếp trên Graph Explorer
POST /YOUR_PAGE_ID/feed
{
  "message": "Test post direct from Graph Explorer",
  "access_token": "YOUR_PAGE_ACCESS_TOKEN"
}
```

#### Kiểm tra Database có đúng không
```sql
-- Check user và social accounts
SELECT u."Email", sa."Provider", st."Name", st."ProviderTargetId"
FROM "Users" u
JOIN "SocialAccounts" sa ON u."Id" = sa."UserId"  
JOIN "SocialTargets" st ON sa."Id" = st."SocialAccountId"
WHERE u."Email" = 'testuser@example.com';
```

#### Debug API Response
```bash
# Check user profile
GET /api/user/profile
Authorization: Bearer YOUR_JWT_TOKEN

# Check social accounts  
GET /api/social/accounts/user/1
Authorization: Bearer YOUR_JWT_TOKEN

# Check targets
GET /api/social/targets/account/1
Authorization: Bearer YOUR_JWT_TOKEN
```

**🎯 Kết quả**: Sau khi hoàn thành các bước trên, bạn có thể đăng bài lên Facebook Page thông qua API mà không cần frontend!
```bash
POST /api/posts
Authorization: Bearer YOUR_JWT_TOKEN

{
  "userId": 1,
  "socialAccountId": 1,
  "socialTargetId": 1,
  "message": "🧪 Test post từ Swagger + Graph API Explorer!",
  "publishImmediately": true
}
```

### Bước 4: Alternative - Mock OAuth Callback

Thay vì frontend, bạn có thể mock OAuth callback:

#### 4.1 Tạo Mock User
```bash
# Tạo user qua API
POST /api/user/register
{
  "email": "mock@example.com",
  "username": "mockuser", 
  "password": "Password123!"
}
```

#### 4.2 Mock Facebook Callback
```bash
# Simulate OAuth callback với access token từ Graph Explorer
GET /auth/facebook/callback?code=mock_code&state=test&userId=1

# Hoặc modify callback handler để accept direct access token
```

### Bước 5: Advanced Testing với Graph API Explorer

#### 5.1 Test Page Posting trực tiếp
```bash
# Trên Graph Explorer, test post trực tiếp
POST /page_id_123/feed
{
  "message": "Test post from Graph Explorer",
  "access_token": "page_access_token"
}
```

#### 5.2 Verify Post đã được tạo
```bash
GET /page_id_123/feed?fields=id,message,created_time
```

#### 5.3 Test với Link và Image
```bash
POST /page_id_123/feed
{
  "message": "Check out this link!",
  "link": "https://example.com",
  "access_token": "page_access_token"
}
```

### 🔧 Debug Tips khi dùng Graph Explorer

#### Check Token Permissions
```bash
GET /me/permissions
# Verify bạn có đủ permissions cần thiết
```

#### Check Token Info  
```bash
GET /debug_token?input_token=YOUR_ACCESS_TOKEN&access_token=YOUR_APP_TOKEN
# Xem token expiry và scopes
```

#### Test Page Access
```bash
GET /page_id/
# Verify bạn có quyền truy cập page này
```

### ⚠️ Lưu ý quan trọng

1. **Access Token Expiry**: Token từ Graph Explorer có thể expire sau 1-2 giờ
2. **Page Tokens**: Page access tokens thường live lâu hơn user tokens
3. **Permissions**: Đảm bảo request đúng permissions trong Graph Explorer
4. **Rate Limiting**: Đừng spam API quá nhiều
5. **Production vs Development**: Graph Explorer tokens chỉ dùng để test

### 🚀 Workflow hoàn chỉnh không cần Frontend

**Tóm tắt**: 
1. Graph Explorer → Generate tokens
2. Manual insert vào DB  
3. Test API qua Swagger
4. Verify trên Facebook Page

Cách này giúp bạn test toàn bộ flow mà không cần implement frontend OAuth!

---

## �🔧 Testing và Debugging

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