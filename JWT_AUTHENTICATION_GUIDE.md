# JWT Authentication System - Migration Guide

## Tổng quan

Hệ thống AISAM đã được chuyển đổi từ Supabase Authentication sang hệ thống JWT Authentication tự viết với đầy đủ cơ chế **Access Token** và **Refresh Token**.

## Thay đổi chính

### 1. Database Schema
- **Bảng `users`**: Đã thêm các trường mới
  - `full_name`: Tên đầy đủ của người dùng
  - `password_hash`: Hash của mật khẩu (HMACSHA512)
  - `password_salt`: Salt cho mật khẩu
  - `is_email_verified`: Trạng thái xác thực email
  - `updated_at`: Thời gian cập nhật
  - `last_login_at`: Thời gian đăng nhập cuối cùng

- **Bảng `sessions`** (mới): Quản lý refresh tokens
  - `id`: UUID primary key
  - `user_id`: Foreign key tới users
  - `refresh_token`: Refresh token (base64, 64 bytes)
  - `expires_at`: Thời gian hết hạn
  - `created_at`: Thời gian tạo
  - `revoked_at`: Thời gian thu hồi (nếu có)
  - `user_agent`: Thông tin trình duyệt
  - `ip_address`: Địa chỉ IP
  - `is_active`: Trạng thái active

### 2. Authentication Flow

#### Access Token
- Thời gian hết hạn: **60 phút** (có thể cấu hình)
- Thuật toán: **HMACSHA256**
- Claims bao gồm:
  - `sub` (NameIdentifier): User ID
  - `email`: Email người dùng
  - `role`: Vai trò (User, Admin, etc.)
  - `name`: Tên đầy đủ (nếu có)
  - `jti`: Token unique identifier

#### Refresh Token
- Thời gian hết hạn: **30 ngày** (có thể cấu hình)
- Lưu trữ trong database (bảng sessions)
- Mỗi refresh token chỉ sử dụng một lần
- Tự động thu hồi token cũ khi tạo token mới

## Cấu hình

### 1. appsettings.json

Thêm section `JwtSettings`:

```json
{
  "JwtSettings": {
    "SecretKey": "YOUR_SECRET_KEY_HERE_MINIMUM_32_CHARACTERS_LONG_FOR_SECURITY",
    "Issuer": "AISAM.API",
    "Audience": "AISAM.Client",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30
  }
}
```

**Lưu ý**: 
- `SecretKey` phải có ít nhất 32 ký tự
- Trong production, sử dụng environment variable hoặc Azure Key Vault
- Đã xóa section `Supabase` (không còn cần thiết cho authentication)

### 2. Environment Variables (khuyến nghị cho production)

```bash
JWT_SECRET_KEY=your-super-secret-key-here-at-least-32-characters-long
JWT_ISSUER=AISAM.API
JWT_AUDIENCE=AISAM.Client
```

## API Endpoints

### 1. Đăng ký (Register)
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!",
  "fullName": "John Doe"
}
```

**Response:**
```json
{
  "success": true,
  "message": "User registered successfully",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "8xK9mP3qR7...",
    "expiresAt": "2026-01-24T15:30:00Z",
    "tokenType": "Bearer",
    "user": {
      "id": "uuid",
      "email": "user@example.com",
      "fullName": "John Doe",
      "role": "User",
      "isEmailVerified": false,
      "createdAt": "2026-01-24T14:30:00Z"
    }
  }
}
```

### 2. Đăng nhập (Login)
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response:** Giống như Register

### 3. Làm mới token (Refresh Token)
```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "8xK9mP3qR7..."
}
```

**Response:** Trả về access token và refresh token mới

### 4. Đăng xuất (Logout)
```http
POST /api/auth/logout
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "refreshToken": "8xK9mP3qR7..."
}
```

### 5. Đăng xuất tất cả thiết bị (Logout All)
```http
POST /api/auth/logout-all
Authorization: Bearer {accessToken}
```

### 6. Xem danh sách sessions đang active
```http
GET /api/auth/sessions
Authorization: Bearer {accessToken}
```

**Response:**
```json
{
  "success": true,
  "message": "Active sessions retrieved successfully",
  "data": [
    {
      "id": "session-uuid",
      "createdAt": "2026-01-24T14:30:00Z",
      "expiresAt": "2026-02-23T14:30:00Z",
      "userAgent": "Mozilla/5.0...",
      "ipAddress": "192.168.1.1",
      "isActive": true
    }
  ]
}
```

### 7. Đổi mật khẩu (Change Password)
```http
POST /api/auth/change-password
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!",
  "confirmPassword": "NewPassword123!"
}
```

**Lưu ý:** Sau khi đổi mật khẩu, tất cả sessions sẽ bị thu hồi và cần đăng nhập lại.

### 8. Lấy thông tin user hiện tại (Get Current User)
```http
GET /api/auth/me
Authorization: Bearer {accessToken}
```

## Migration Database

Chạy migration để cập nhật database:

```bash
cd AISAM.Repositories
dotnet ef database update --startup-project ../AISAM.API/AISAM.API.csproj
```

## Sử dụng trong Client

### 1. Lưu trữ tokens

**Khuyến nghị:**
- **Access Token**: Lưu trong memory (React state, Vue data, etc.)
- **Refresh Token**: Lưu trong HttpOnly Cookie (an toàn hơn) hoặc localStorage

### 2. Gửi request với authentication

```javascript
// Thêm access token vào header
fetch('https://api.aisam.com/api/profile', {
  headers: {
    'Authorization': `Bearer ${accessToken}`,
    'Content-Type': 'application/json'
  }
})
```

### 3. Tự động refresh token khi hết hạn

```javascript
// Interceptor example (Axios)
axios.interceptors.response.use(
  response => response,
  async error => {
    if (error.response?.status === 401) {
      // Access token hết hạn
      try {
        const response = await axios.post('/api/auth/refresh', {
          refreshToken: getRefreshToken()
        });
        
        const { accessToken, refreshToken } = response.data.data;
        
        // Lưu tokens mới
        setAccessToken(accessToken);
        setRefreshToken(refreshToken);
        
        // Retry request ban đầu
        error.config.headers.Authorization = `Bearer ${accessToken}`;
        return axios.request(error.config);
      } catch (refreshError) {
        // Refresh token cũng hết hạn, redirect to login
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);
```

## Security Best Practices

### 1. Secret Key
- Sử dụng key ngẫu nhiên, độ dài tối thiểu 256 bits (32 bytes)
- Không commit vào Git
- Sử dụng environment variables hoặc secret management service

### 2. HTTPS
- Bắt buộc sử dụng HTTPS trong production
- Update `RequireHttpsMetadata = true` trong JwtBearer options

### 3. Password Policy
- Tối thiểu 8 ký tự
- Khuyến nghị: Kết hợp chữ hoa, chữ thường, số và ký tự đặc biệt
- Có thể thêm validation rules trong `RegisterRequest`

### 4. Rate Limiting
- Khuyến nghị thêm rate limiting cho các endpoints:
  - `/api/auth/login`: 5 requests/minute
  - `/api/auth/register`: 3 requests/minute
  - `/api/auth/refresh`: 10 requests/minute

### 5. Session Management
- Tự động xóa sessions hết hạn (có thể thêm background job)
- Giới hạn số sessions đồng thời cho mỗi user
- Cung cấp tính năng "trusted devices"

## Troubleshooting

### 1. "JWT SecretKey is not configured"
- Kiểm tra `appsettings.json` có section `JwtSettings`
- Kiểm tra `SecretKey` không rỗng

### 2. "Invalid or expired refresh token"
- Refresh token đã hết hạn hoặc đã bị thu hồi
- User cần đăng nhập lại

### 3. "Token validation failed"
- Kiểm tra `Issuer` và `Audience` trong token match với config
- Kiểm tra `SecretKey` đúng
- Kiểm tra token chưa hết hạn

## Các files đã thay đổi

### Models
- `AISAM.Data/Model/User.cs` - Cập nhật
- `AISAM.Data/Model/Session.cs` - Mới

### Repositories
- `AISAM.Repositories/IRepositories/ISessionRepository.cs` - Mới
- `AISAM.Repositories/Repository/SessionRepository.cs` - Mới
- `AISAM.Repositories/AISAMContext.cs` - Cập nhật

### Services
- `AISAM.Services/IServices/IAuthService.cs` - Mới
- `AISAM.Services/Service/AuthService.cs` - Mới

### DTOs
- `AISAM.Common/Dtos/Request/AuthRequest.cs` - Mới
- `AISAM.Common/Dtos/Response/AuthResponse.cs` - Mới
- `AISAM.Common/Config/JwtSettings.cs` - Mới

### Controllers
- `AISAM.API/Controllers/AuthController.cs` - Mới

### Configuration
- `AISAM.API/Program.cs` - Cập nhật (JWT config, loại bỏ Supabase auth)
- `AISAM.API/appsettings.json` - Cập nhật

## Lưu ý về Supabase Storage

**Quan trọng:** Hệ thống vẫn giữ Supabase client cho **storage** (lưu trữ files). Chỉ có **authentication** được chuyển sang JWT tự viết.

Nếu muốn migrate storage sang solution khác (Azure Blob, AWS S3, etc.), cần:
1. Cập nhật `SupabaseStorageService`
2. Migrate existing files
3. Update environment variables

## Hỗ trợ

Nếu gặp vấn đề, vui lòng:
1. Kiểm tra logs trong console
2. Verify database migration đã chạy thành công
3. Kiểm tra configuration trong `appsettings.json`
4. Kiểm tra JWT token bằng [jwt.io](https://jwt.io)
