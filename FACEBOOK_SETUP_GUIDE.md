# Facebook OAuth Setup Guide

## Lỗi hiện tại
Bạn đang gặp lỗi **400 Bad Request** khi trao đổi authorization code để lấy access token từ Facebook. Đây là các bước để khắc phục:

## Bước 1: Tạo Facebook App

1. Truy cập [Facebook Developers](https://developers.facebook.com/)
2. Đăng nhập bằng tài khoản Facebook của bạn
3. Click **"Create App"**
4. Chọn **"Business"** hoặc **"Consumer"** (khuyến nghị Business)
5. Điền thông tin app:
   - **App Name**: AISAM Social Media Manager
   - **App Contact Email**: email của bạn
   - **Business Account**: chọn hoặc tạo mới

## Bước 2: Cấu hình Facebook Login

1. Trong App Dashboard, tìm **"Add a Product"**
2. Chọn **"Facebook Login"** và click **"Set Up"**
3. Chọn **"Web"** platform
4. Nhập **Site URL**: `http://localhost:5283`

## Bước 3: Cấu hình OAuth Redirect URIs

1. Trong **Facebook Login > Settings**
2. Thêm vào **"Valid OAuth Redirect URIs"**:
   ```
   http://localhost:5283/api/social-auth/facebook/callback
   ```
3. **Lưu ý**: URI này phải khớp chính xác với `RedirectUri` trong `appsettings.json`

## Bước 4: Lấy App ID và App Secret

1. Trong **App Dashboard > Settings > Basic**
2. Copy **App ID** và **App Secret**
3. Cập nhật file `appsettings.json`:

```json
{
  "FacebookSettings": {
    "AppId": "YOUR_ACTUAL_APP_ID_HERE",
    "AppSecret": "YOUR_ACTUAL_APP_SECRET_HERE",
    "RedirectUri": "http://localhost:5283/api/social-auth/facebook/callback",
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

## Bước 5: Cấu hình App Permissions

1. Trong **App Dashboard > Products > Facebook Login > Settings**
2. Thêm **Permissions** cần thiết:
   - `pages_manage_posts`
   - `pages_read_engagement`
   - `pages_show_list`
   - `email`
   - `public_profile`

## Bước 6: Cấu hình App Review (nếu cần)

- Với permissions như `pages_manage_posts`, bạn có thể cần submit app để review
- Trong giai đoạn development, bạn có thể thêm test users

## Bước 7: Test OAuth Flow

1. Restart ứng dụng sau khi cập nhật `appsettings.json`
2. Test bằng cách gọi:
   ```bash
   curl -X GET "http://localhost:5283/api/social-auth/facebook"
   ```
3. Sử dụng URL trả về để test OAuth flow

## Lưu ý quan trọng

1. **Redirect URI phải khớp chính xác** - đây là nguyên nhân phổ biến nhất gây lỗi 400
2. **Authorization code chỉ sử dụng được một lần** - nếu test nhiều lần, cần lấy code mới
3. **Code có thời gian hết hạn ngắn** - thường là 10 phút
4. **App phải ở chế độ Development** để test với localhost

## Troubleshooting

### Lỗi 400 Bad Request
- Kiểm tra App ID và App Secret
- Kiểm tra Redirect URI có khớp không
- Kiểm tra code có hết hạn không

### Lỗi 403 Forbidden  
- Kiểm tra App permissions
- Kiểm tra App có ở chế độ Development không

### Lỗi Invalid Redirect URI
- Đảm bảo Redirect URI trong Facebook App khớp với `appsettings.json`
- Không có trailing slash hoặc protocol khác nhau

## Test Commands

```bash
# 1. Lấy authorization URL
curl -X GET "http://localhost:5283/api/social-auth/facebook"

# 2. Test callback (thay YOUR_CODE bằng code thực từ Facebook)
curl -X GET "http://localhost:5283/api/social-auth/facebook/callback?code=YOUR_CODE&userId=b79f7cce-2abd-4f9c-9a24-950f46bd20df"
```
