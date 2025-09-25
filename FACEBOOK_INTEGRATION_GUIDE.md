# Hướng Dẫn Triển Khai Liên Kết Facebook với User Đang Đăng Nhập

## Tổng Quan

Tài liệu này hướng dẫn cách triển khai tính năng liên kết tài khoản Facebook với user đang đăng nhập trong hệ thống AISAM. Hệ thống hỗ trợ OAuth 2.0 flow để liên kết tài khoản Facebook và quản lý các trang Facebook của user.

## Kiến Trúc Hệ Thống

### Backend Components
- **AuthController**: Xử lý đăng nhập/đăng ký user
- **SocialAuthController**: Xử lý OAuth flow cho các mạng xã hội
- **SocialAccountsController**: Quản lý tài khoản mạng xã hội đã liên kết
- **UserService**: Quản lý thông tin user
- **SocialService**: Xử lý logic liên kết mạng xã hội

### Database Models
- **User**: Thông tin user chính
- **SocialAccount**: Tài khoản mạng xã hội đã liên kết
- **SocialTarget**: Các trang/trang con trong tài khoản mạng xã hội

## Cấu Hình Facebook App

### 1. Tạo Facebook App
1. Truy cập [Facebook Developers](https://developers.facebook.com/)
2. Tạo app mới với loại "Business"
3. Thêm sản phẩm "Facebook Login"

### 2. Cấu Hình OAuth Settings
```
Valid OAuth Redirect URIs:
- https://yourdomain.com/api/social-auth/facebook/callback
- http://localhost:5000/api/social-auth/facebook/callback (cho development)
```

### 3. Cấu Hình App Settings
```json
{
  "FacebookSettings": {
    "AppId": "YOUR_FACEBOOK_APP_ID",
    "AppSecret": "YOUR_FACEBOOK_APP_SECRET",
    "RedirectUri": "https://yourdomain.com/api/social-auth/facebook/callback",
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

## API Endpoints (Đã Triển Khai)

### 1. Lấy URL Authorization Facebook

**Endpoint:** `GET /api/social-auth/{provider}`

**Mô tả:** Lấy URL để redirect user đến Facebook OAuth

**Query Parameters:**
- `state` (optional): State parameter để bảo mật

**Response:**
```json
{
  "success": true,
  "data": {
    "authUrl": "https://www.facebook.com/v20.0/dialog/oauth?client_id=...",
    "state": "random_state_string"
  }
}
```

**Ví dụ Frontend:**
```javascript
// Lấy authorization URL cho Facebook
const response = await fetch('/api/social-auth/facebook?state=random_state');
const data = await response.json();
window.location.href = data.data.authUrl;
```

### 2. Xử Lý Callback từ Facebook

**Endpoint:** `GET /api/social-auth/{provider}/callback`

**Mô tả:** Xử lý callback từ Facebook sau khi user authorize

**Query Parameters:**
- `code`: Authorization code từ Facebook (required)
- `state`: State parameter (optional)
- `userId`: ID của user đang đăng nhập (optional - nếu không có sẽ tạo demo user)

**Response:**
```json
{
  "success": true,
  "data": {
    "user": {
      "id": "user-guid",
      "email": "user@example.com",
      "createdAt": "2024-01-01T00:00:00Z"
    },
    "socialAccount": {
      "id": "social-account-guid",
      "provider": "facebook",
      "providerUserId": "facebook-user-id",
      "isActive": true,
      "expiresAt": "2024-02-01T00:00:00Z",
      "createdAt": "2024-01-01T00:00:00Z",
      "targets": [
        {
          "id": "target-guid",
          "providerTargetId": "facebook-page-id",
          "name": "My Facebook Page",
          "type": "page",
          "category": "Business",
          "profilePictureUrl": "https://...",
          "isActive": true
        }
      ]
    },
    "message": "facebook account linked successfully"
  }
}
```

### 3. Liên Kết Tài Khoản Facebook (Manual)

**Endpoint:** `POST /api/social-auth/link`

**Mô tả:** Liên kết tài khoản Facebook với user bằng authorization code

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "userId": "user-guid",
  "provider": "facebook",
  "code": "authorization_code_from_facebook",
  "state": "optional_state"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "social-account-guid",
    "provider": "facebook",
    "providerUserId": "facebook-user-id",
    "isActive": true,
    "expiresAt": "2024-02-01T00:00:00Z",
    "createdAt": "2024-01-01T00:00:00Z",
    "targets": [...]
  }
}
```

### 4. Liên Kết Facebook Page bằng Token (Temporary)

**Endpoint:** `POST /api/social-auth/link-page-token`

**Mô tả:** **[TEMP]** Liên kết Facebook Page trực tiếp bằng Page Access Token. Đây là endpoint tạm thời để test khả năng posting.

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "userId": "user-guid",
  "pageAccessToken": "facebook_page_access_token",
  "userAccessToken": "facebook_user_access_token (optional)"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "social-account-guid",
    "provider": "facebook",
    "providerUserId": "facebook-user-id",
    "isActive": true,
    "expiresAt": null,
    "createdAt": "2024-01-01T00:00:00Z",
    "targets": [
      {
        "id": "target-guid",
        "providerTargetId": "facebook-page-id",
        "name": "My Facebook Page",
        "type": "page",
        "category": "Business",
        "profilePictureUrl": "https://...",
        "isActive": true
      }
    ]
  },
  "message": "Facebook page linked successfully"
}
```

### 5. Lấy Danh Sách Tài Khoản Mạng Xã Hội

**Endpoint:** `GET /api/social/accounts/user/{userId}`

**Mô tả:** Lấy danh sách tất cả tài khoản mạng xã hội của user

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "social-account-guid",
      "provider": "facebook",
      "providerUserId": "facebook-user-id",
      "isActive": true,
      "expiresAt": "2024-02-01T00:00:00Z",
      "createdAt": "2024-01-01T00:00:00Z",
      "targets": [
        {
          "id": "target-guid",
          "providerTargetId": "facebook-page-id",
          "name": "My Facebook Page",
          "type": "page",
          "category": "Business",
          "profilePictureUrl": "https://...",
          "isActive": true
        }
      ]
    }
  ]
}
```

### 6. Hủy Liên Kết Tài Khoản

**Endpoint:** `DELETE /api/social-auth/unlink/{userId}/{socialAccountId}`

**Mô tả:** Hủy liên kết tài khoản mạng xã hội. **Lưu ý:** Khi hủy liên kết, tất cả posts liên quan đến tài khoản này sẽ bị xóa.

**Headers:**
```
Authorization: Bearer {access_token}
```

**Response:**
```json
{
  "success": true,
  "message": "Social account unlinked successfully"
}
```

**Lưu ý quan trọng:**
- Khi hủy liên kết tài khoản mạng xã hội, hệ thống sẽ tự động xóa:
  1. Tất cả posts đã tạo từ tài khoản này
  2. Tất cả social targets (pages) liên quan
  3. Cuối cùng mới xóa social account
- Hành động này không thể hoàn tác, hãy thông báo rõ ràng cho user trước khi thực hiện

## Lưu Ý Quan Trọng về API

### ⚠️ **API Tạm Thời**
- **`POST /api/social-auth/link-page-token`** là API tạm thời để test khả năng posting
- API này sẽ được thay thế bằng OAuth flow chính thức trong tương lai
- Hiện tại có thể sử dụng để liên kết Facebook Page trực tiếp bằng Page Access Token

### 🔄 **OAuth Flow Chính Thức**
- **`GET /api/social-auth/{provider}`** - Lấy authorization URL
- **`GET /api/social-auth/{provider}/callback`** - Xử lý callback từ Facebook
- **`POST /api/social-auth/link`** - Liên kết tài khoản bằng authorization code

## Flow Triển Khai Frontend

### 1. Đăng Nhập User
```javascript
// Đăng nhập user trước
const loginResponse = await fetch('/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'password'
  })
});

const loginData = await loginResponse.json();
const accessToken = loginData.data.accessToken;
```

### 2. Liên Kết Facebook Account
```javascript
async function linkFacebookAccount() {
  try {
    // Bước 1: Lấy authorization URL
    const authResponse = await fetch('/api/social-auth/facebook?state=random_state');
    const authData = await authResponse.json();
    
    if (authData.success) {
      // Bước 2: Redirect user đến Facebook
      window.location.href = authData.data.authUrl;
    } else {
      console.error('Failed to get auth URL:', authData.message);
    }
    
  } catch (error) {
    console.error('Error linking Facebook account:', error);
  }
}
```

### 3. Xử Lý Callback (Trong Popup hoặc Redirect)
```javascript
// Nếu sử dụng popup
function openFacebookAuth() {
  const popup = window.open(
    authData.data.authUrl,
    'facebook-auth',
    'width=600,height=600'
  );
  
  // Lắng nghe message từ popup
  window.addEventListener('message', (event) => {
    if (event.origin !== window.location.origin) return;
    
    if (event.data.type === 'FACEBOOK_AUTH_SUCCESS') {
      // Cập nhật UI với tài khoản đã liên kết
      loadUserSocialAccounts();
      popup.close();
    }
  });
}

// Nếu sử dụng redirect, xử lý trong callback page
// callback.html
window.addEventListener('load', async () => {
  const urlParams = new URLSearchParams(window.location.search);
  const code = urlParams.get('code');
  const state = urlParams.get('state');
  
  if (code) {
    try {
      // Gọi callback API với userId của user đang đăng nhập
      const response = await fetch(`/api/social-auth/facebook/callback?code=${code}&state=${state}&userId=${userId}`);
      const data = await response.json();
      
      if (data.success) {
        // Thông báo thành công và đóng popup
        window.opener.postMessage({
          type: 'FACEBOOK_AUTH_SUCCESS',
          data: data.data
        }, window.location.origin);
        window.close();
      } else {
        console.error('Facebook callback failed:', data.message);
        alert('Failed to link Facebook account: ' + data.message);
      }
    } catch (error) {
      console.error('Error processing Facebook callback:', error);
      alert('An error occurred while linking Facebook account.');
    }
  }
});
```

### 4. Hiển Thị Tài Khoản Đã Liên Kết
```javascript
async function loadUserSocialAccounts() {
  try {
    const response = await fetch(`/api/social/accounts/user/${userId}`);
    const data = await response.json();
    
    if (data.success) {
      displaySocialAccounts(data.data);
    } else {
      console.error('Failed to load social accounts:', data.message);
    }
  } catch (error) {
    console.error('Error loading social accounts:', error);
  }
}

function displaySocialAccounts(accounts) {
  const container = document.getElementById('social-accounts');
  container.innerHTML = '';
  
  accounts.forEach(account => {
    if (account.provider === 'facebook') {
      const accountElement = document.createElement('div');
      accountElement.innerHTML = `
        <div class="social-account">
          <h3>Facebook Account</h3>
          <p>User ID: ${account.providerUserId}</p>
          <p>Pages: ${account.targets.length}</p>
          <button onclick="unlinkAccount('${account.id}')">Unlink</button>
        </div>
      `;
      container.appendChild(accountElement);
    }
  });
}
```

### 5. Hủy Liên Kết Tài Khoản
```javascript
async function unlinkAccount(socialAccountId) {
  // Cảnh báo rõ ràng về việc xóa posts
  const confirmed = confirm(
    'Are you sure you want to unlink this account?\n\n' +
    'WARNING: This will permanently delete:\n' +
    '• All posts created from this account\n' +
    '• All linked pages/targets\n' +
    '• This action cannot be undone'
  );
  
  if (!confirmed) {
    return;
  }
  
  try {
    const response = await fetch(`/api/social-auth/unlink/${userId}/${socialAccountId}`, {
      method: 'DELETE'
    });
    
    const data = await response.json();
    
    if (data.success) {
      // Reload danh sách tài khoản
      loadUserSocialAccounts();
      alert('Account unlinked successfully. All related posts have been deleted.');
    } else {
      alert('Failed to unlink account: ' + data.message);
    }
  } catch (error) {
    console.error('Error unlinking account:', error);
    alert('An error occurred while unlinking the account.');
  }
}
```

## Error Handling

### Common Error Responses
```json
{
  "success": false,
  "message": "Error message",
  "statusCode": 400,
  "errorCode": "INVALID_REQUEST"
}
```

### Error Codes
- `INVALID_CREDENTIALS`: Thông tin đăng nhập không đúng
- `ACCOUNT_NOT_ACTIVE`: Tài khoản bị vô hiệu hóa
- `FACEBOOK_AUTH_FAILED`: Lỗi xác thực Facebook
- `ACCOUNT_ALREADY_LINKED`: Tài khoản đã được liên kết
- `INVALID_ACCESS_TOKEN`: Token không hợp lệ

## Security Considerations

### 1. Token Management
- Access tokens được mã hóa khi lưu trữ
- Refresh tokens được sử dụng để gia hạn access tokens
- Tokens có thời hạn và được kiểm tra định kỳ

### 2. State Parameter
- Sử dụng state parameter để ngăn chặn CSRF attacks
- State được generate ngẫu nhiên và validate trong callback

### 3. HTTPS
- Tất cả API calls phải sử dụng HTTPS
- Facebook OAuth chỉ hoạt động với HTTPS trong production

### 4. Data Deletion & Cascade Operations
- Khi hủy liên kết tài khoản, hệ thống thực hiện cascade delete:
  1. Xóa tất cả SocialPosts liên quan
  2. Xóa tất cả SocialTargets (pages)
  3. Cuối cùng xóa SocialAccount
- Đảm bảo user được thông báo rõ ràng về hậu quả của việc hủy liên kết
- Log tất cả các thao tác xóa để audit trail

## Testing

### 1. Development Environment
```bash
# Start API server
dotnet run --project AISAM.API

# Test endpoints
curl -X GET "https://localhost:5001/api/social-auth/facebook" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

### 2. Facebook Test Users
- Sử dụng Facebook Test Users để test OAuth flow
- Tạo test pages để test page management features

## Troubleshooting

### 1. Common Issues
- **Invalid Redirect URI**: Kiểm tra cấu hình trong Facebook App Settings
- **Invalid App ID/Secret**: Verify Facebook app credentials
- **Token Expired**: Implement token refresh logic
- **Permission Denied**: Kiểm tra required permissions trong Facebook app

### 2. Debug Tips
- Enable detailed logging trong development
- Sử dụng Facebook Graph API Explorer để test API calls
- Kiểm tra network requests trong browser dev tools

## Next Steps

1. **Implement Token Refresh**: Tự động refresh Facebook access tokens
2. **Add More Providers**: Mở rộng cho Instagram, TikTok
3. **Enhanced Error Handling**: Cải thiện error messages và recovery
4. **Analytics**: Thêm tracking cho social account linking
5. **Bulk Operations**: Hỗ trợ quản lý nhiều tài khoản cùng lúc

---

*Tài liệu này được cập nhật lần cuối: [Ngày hiện tại]*
*Phiên bản API: v1.0*
