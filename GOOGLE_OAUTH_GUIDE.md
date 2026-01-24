# Google OAuth Integration - Setup Guide

## Tổng quan

Hệ thống AISAM.API đã được tích hợp **Google OAuth 2.0** để hỗ trợ đăng nhập và quản lý YouTube channels. Người dùng có thể kết nối tài khoản Google của họ để publish content lên YouTube.

## Tính năng

✅ **OAuth 2.0 Authentication** với Google
✅ **YouTube Channel Management** - Lấy danh sách và quản lý channels
✅ **Access Token Management** - Tự động refresh token khi hết hạn
✅ **Refresh Token Support** - Token dài hạn (30 ngày)
✅ **Multi-Channel Support** - Hỗ trợ nhiều YouTube channels cho mỗi user

## Cấu hình Google OAuth

### 1. Tạo Google OAuth Credentials

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Tạo hoặc chọn một project
3. Enable **YouTube Data API v3**:
   - Vào **APIs & Services > Library**
   - Tìm "YouTube Data API v3"
   - Click **Enable**

4. Tạo OAuth 2.0 Credentials:
   - Vào **APIs & Services > Credentials**
   - Click **Create Credentials > OAuth 2.0 Client ID**
   - Chọn **Web application**
   - Thêm **Authorized redirect URIs**:
     ```
     http://localhost:3000/auth/google/callback
     https://yourdomain.com/auth/google/callback
     ```
   - Click **Create**
   - Lưu lại **Client ID** và **Client Secret**

5. Configure OAuth consent screen:
   - Vào **OAuth consent screen**
   - Chọn **External** (hoặc Internal nếu G Suite)
   - Điền thông tin app
   - Thêm **Scopes**:
     - `openid`
     - `email`
     - `profile`
     - `https://www.googleapis.com/auth/youtube.readonly`
     - `https://www.googleapis.com/auth/youtube.upload`
     - `https://www.googleapis.com/auth/youtube.force-ssl`

### 2. Cấu hình trong appsettings.json

```json
{
  "GoogleSettings": {
    "ClientId": "YOUR_CLIENT_ID.apps.googleusercontent.com",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "RedirectUri": "http://localhost:3000/auth/google/callback",
    "RequiredScopes": [
      "openid",
      "email",
      "profile",
      "https://www.googleapis.com/auth/youtube.readonly",
      "https://www.googleapis.com/auth/youtube.upload",
      "https://www.googleapis.com/auth/youtube.force-ssl"
    ]
  }
}
```

### 3. Environment Variables (Production)

```bash
GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your-client-secret
```

## API Endpoints

### 1. Get OAuth Authorization URL

```http
GET /api/social-auth/google
Authorization: Bearer {accessToken}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "authUrl": "https://accounts.google.com/o/oauth2/v2/auth?client_id=...",
    "state": "random-state-string"
  }
}
```

### 2. Handle OAuth Callback

Sau khi user authorize trên Google, redirect về frontend với `code` và `state`. Frontend gọi API:

```http
POST /api/social-auth/google/callback
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "code": "4/0AfJohXm...",
  "state": "random-state-string"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Tài khoản google đã được liên kết thành công",
  "data": {
    "socialAccount": {
      "id": "uuid",
      "profileId": "uuid",
      "provider": "google",
      "providerUserId": "117...",
      "accessToken": "ya29.a0AfH6...",
      "isActive": true,
      "expiresAt": "2026-01-24T15:30:00Z"
    },
    "availableTargets": [
      {
        "providerTargetId": "UCxxx...",
        "name": "My YouTube Channel",
        "type": "youtube_channel",
        "profilePictureUrl": "https://yt3.ggpht.com/...",
        "isActive": true
      }
    ]
  }
}
```

### 3. Link YouTube Channels to Brand

```http
POST /api/social-integration/link-targets
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "profileId": "uuid",
  "provider": "google",
  "providerTargetIds": ["UCxxx...", "UCyyy..."],
  "brandId": "uuid"
}
```

### 4. List Connected YouTube Channels

```http
GET /api/social-integration?profileId={profileId}&provider=google
Authorization: Bearer {accessToken}
```

## Frontend Integration

### 1. React Example

```typescript
// Step 1: Get OAuth URL
const initiateGoogleAuth = async () => {
  try {
    const response = await fetch('/api/social-auth/google', {
      headers: {
        'Authorization': `Bearer ${accessToken}`
      }
    });
    
    const data = await response.json();
    
    // Store state for verification
    sessionStorage.setItem('google_oauth_state', data.data.state);
    
    // Redirect to Google OAuth
    window.location.href = data.data.authUrl;
  } catch (error) {
    console.error('Failed to initiate Google auth:', error);
  }
};

// Step 2: Handle Callback
const handleGoogleCallback = async (code: string, state: string) => {
  try {
    // Verify state
    const savedState = sessionStorage.getItem('google_oauth_state');
    if (state !== savedState) {
      throw new Error('Invalid state parameter');
    }
    
    const response = await fetch('/api/social-auth/google/callback', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${accessToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ code, state })
    });
    
    const data = await response.json();
    
    if (data.success) {
      console.log('Connected channels:', data.data.availableTargets);
      // Show channel selection UI
      showChannelSelection(data.data.availableTargets);
    }
  } catch (error) {
    console.error('Failed to handle Google callback:', error);
  }
};

// Step 3: In your callback route component
const GoogleCallbackPage = () => {
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const code = params.get('code');
    const state = params.get('state');
    
    if (code && state) {
      handleGoogleCallback(code, state);
    }
  }, []);
  
  return <div>Connecting to Google...</div>;
};
```

### 2. Vue.js Example

```vue
<template>
  <div>
    <button @click="connectGoogle">Connect Google</button>
    
    <div v-if="channels.length > 0">
      <h3>Select YouTube Channels</h3>
      <div v-for="channel in channels" :key="channel.providerTargetId">
        <input 
          type="checkbox" 
          :value="channel.providerTargetId"
          v-model="selectedChannels"
        />
        <label>{{ channel.name }}</label>
      </div>
      <button @click="linkChannels">Link Selected Channels</button>
    </div>
  </div>
</template>

<script>
export default {
  data() {
    return {
      channels: [],
      selectedChannels: []
    };
  },
  methods: {
    async connectGoogle() {
      const response = await fetch('/api/social-auth/google', {
        headers: {
          'Authorization': `Bearer ${this.$store.state.accessToken}`
        }
      });
      
      const data = await response.json();
      sessionStorage.setItem('google_oauth_state', data.data.state);
      window.location.href = data.data.authUrl;
    },
    
    async linkChannels() {
      await fetch('/api/social-integration/link-targets', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.$store.state.accessToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          profileId: this.$store.state.profileId,
          provider: 'google',
          providerTargetIds: this.selectedChannels,
          brandId: this.$store.state.brandId
        })
      });
    }
  },
  
  mounted() {
    // Handle OAuth callback
    const urlParams = new URLSearchParams(window.location.search);
    const code = urlParams.get('code');
    const state = urlParams.get('state');
    
    if (code && state) {
      this.handleCallback(code, state);
    }
  }
};
</script>
```

## Database Schema

### SocialAccount (updated)
```sql
-- Thêm Google vào enum
ALTER TYPE social_platform_enum ADD VALUE IF NOT EXISTS 'Google';
ALTER TYPE social_platform_enum ADD VALUE IF NOT EXISTS 'YouTube';

-- SocialAccount sẽ lưu user access token và refresh token
SELECT * FROM social_accounts 
WHERE platform = 4; -- 4 = Google
```

### SocialIntegration
```sql
-- YouTube channels được lưu trong social_integrations
-- Mỗi channel là một integration riêng
SELECT * FROM social_integrations 
WHERE platform = 4 AND external_id = 'UCxxx...';
```

## Scopes Giải thích

| Scope | Mục đích |
|-------|----------|
| `openid` | Xác thực user identity |
| `email` | Lấy email của user |
| `profile` | Lấy thông tin profile (name, picture) |
| `youtube.readonly` | Đọc thông tin YouTube channels |
| `youtube.upload` | Upload video lên YouTube |
| `youtube.force-ssl` | Truy cập YouTube API qua HTTPS |

## Token Management

### Access Token
- **Thời gian hết hạn**: 1 giờ
- **Tự động refresh**: Khi còn < 5 phút
- **Lưu trữ**: Database (encrypted khuyến nghị)

### Refresh Token
- **Thời gian hết hạn**: Không hết hạn (cho đến khi revoke)
- **Lấy lần đầu**: Phải có `access_type=offline` và `prompt=consent`
- **Lưu trữ**: Database (encrypted)

### Refresh Token Flow

```csharp
// Tự động refresh trong GoogleProvider
await RefreshTokenIfNeededAsync(account);

// Hoặc manual refresh
var refreshRequest = new Dictionary<string, string>
{
    { "client_id", _settings.ClientId },
    { "client_secret", _settings.ClientSecret },
    { "refresh_token", account.RefreshToken },
    { "grant_type", "refresh_token" }
};
```

## YouTube Publishing (Future)

Hiện tại `PublishAsync` chưa được implement đầy đủ. Để implement:

1. **Upload video file** using [Resumable Upload](https://developers.google.com/youtube/v3/guides/using_resumable_upload_protocol)
2. **Set video metadata**: title, description, tags, privacy
3. **Handle thumbnails**
4. **Monitor upload progress**

Example implementation:
```csharp
// POST https://www.googleapis.com/upload/youtube/v3/videos
// Content-Type: multipart/form-data
// Authorization: Bearer {access_token}
```

## Testing

### 1. Test OAuth Flow
```bash
# 1. Get auth URL
curl -X GET http://localhost:5283/api/social-auth/google \
  -H "Authorization: Bearer {token}"

# 2. Open URL in browser, authorize
# 3. Get code from redirect URL

# 4. Exchange code
curl -X POST http://localhost:5283/api/social-auth/google/callback \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"code": "4/0AfJoh...", "state": "..."}'
```

### 2. Test Token Refresh
```bash
# Manually trigger refresh by setting ExpiresAt in past
UPDATE social_accounts 
SET expires_at = NOW() - INTERVAL '1 hour'
WHERE platform = 4;

# Then make API call - should auto refresh
```

## Security Best Practices

1. **Always use HTTPS** in production
2. **Validate state parameter** để prevent CSRF
3. **Store tokens encrypted** trong database
4. **Implement rate limiting** cho OAuth endpoints
5. **Revoke tokens** khi user disconnect account
6. **Monitor quota usage** - YouTube API có limits
7. **Handle token expiration** gracefully

## Quota & Limits

YouTube Data API có các giới hạn:
- **Quota per day**: 10,000 units (có thể request tăng)
- **Video upload**: 1,600 units per video
- **List channels**: 1 unit per request
- **Rate limit**: ~100 requests per 100 seconds

## Troubleshooting

### "Access blocked: This app is not verified"
- App đang ở trạng thái testing
- Thêm test users vào OAuth consent screen
- Hoặc submit app để verify

### "Invalid grant"
- Refresh token đã expire hoặc revoked
- User cần re-authenticate
- Check nếu user đã revoke access trong Google settings

### "Insufficient permissions"
- Check scopes trong OAuth consent screen
- User phải consent lại nếu scopes thay đổi
- Verify `prompt=consent` trong auth URL

### "Channel not found"
- User chưa có YouTube channel
- Return empty list trong `GetTargetsAsync`

## Resources

- [Google OAuth 2.0](https://developers.google.com/identity/protocols/oauth2)
- [YouTube Data API v3](https://developers.google.com/youtube/v3)
- [OAuth 2.0 Playground](https://developers.google.com/oauthplayground/)
- [Google Cloud Console](https://console.cloud.google.com/)

## Next Steps

1. ✅ Đã hoàn thành OAuth flow
2. ✅ Đã hoàn thành channel listing
3. ⏳ Implement YouTube video upload
4. ⏳ Implement video scheduling
5. ⏳ Implement analytics tracking
