# Facebook Marketing API Test Examples

## 📋 Overview
Các JSON request mẫu để test Facebook Marketing API v23.0 với sandbox mode và production mode.

## 🔧 Configuration
- **Sandbox Mode**: VND currency, budget không cần convert
- **Production Mode**: USD currency, budget convert to cents (x100)
- **API Version**: v23.0
- **Base URL**: https://graph.facebook.com

---

## 1. 🎯 Create Ad Campaign

### Request
```http
POST /api/ad-campaigns
Content-Type: application/json
Authorization: Bearer {your_jwt_token}
```

### JSON Body
```json
{
  "brandId": "c5b6337b-17ab-4842-82e7-a81a90b8c8aa",
  "adAccountId": "4005413613009650",
  "name": "Test Campaign - Video Views",
  "objective": "VIDEO_VIEWS",
  "budget": 100,
  "startDate": "2025-01-20T00:00:00.000Z",
  "endDate": "2025-01-25T23:59:59.000Z"
}
```

### Expected Response
```json
{
  "success": true,
  "message": "Campaign created successfully",
  "data": {
    "id": "f1e2143c-2574-499b-86f5-f571af4cbc45",
    "userId": "c9f3fbe8-aca3-4101-bd9e-a5e730dfcbf5",
    "brandId": "c5b6337b-17ab-4842-82e7-a81a90b8c8aa",
    "adAccountId": "4005413613009650",
    "facebookCampaignId": "23851261137850792",
    "name": "Test Campaign - Video Views",
    "objective": "VIDEO_VIEWS",
    "budget": 100,
    "startDate": "2025-01-20T00:00:00.000Z",
    "endDate": "2025-01-25T23:59:59.000Z",
    "isActive": true,
    "createdAt": "2025-01-20T10:30:00.000Z",
    "updatedAt": "2025-01-20T10:30:00.000Z",
    "adSets": []
  }
}
```

---

## 2. 📊 Create Ad Set

### Request
```http
POST /api/ad-sets
Content-Type: application/json
Authorization: Bearer {your_jwt_token}
```

### JSON Body
```json
{
  "campaignId": "f1e2143c-2574-499b-86f5-f571af4cbc45",
  "name": "Test Ad Set - Vietnam Targeting",
  "targeting": {
    "geo_locations": {
      "countries": ["VN"]
    },
    "age_min": 18,
    "age_max": 35,
    "genders": ["MALE", "FEMALE"],
    "interests": [
      {
        "id": "6003107902433",
        "name": "Technology"
      },
      {
        "id": "6004037226461",
        "name": "Mobile phones"
      }
    ]
  },
  "dailyBudget": 50,
  "startDate": "2025-01-20T00:00:00.000Z",
  "endDate": "2025-01-25T23:59:59.000Z"
}
```

### Expected Response
```json
{
  "success": true,
  "message": "Ad set created successfully",
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "campaignId": "f1e2143c-2574-499b-86f5-f571af4cbc45",
    "facebookAdSetId": "23851261137850801",
    "name": "Test Ad Set - Vietnam Targeting",
    "targeting": "{\"geo_locations\":{\"countries\":[\"VN\"]},\"age_min\":18,\"age_max\":35,\"genders\":[\"MALE\",\"FEMALE\"],\"interests\":[{\"id\":\"6003107902433\",\"name\":\"Technology\"},{\"id\":\"6004037226461\",\"name\":\"Mobile phones\"}]}",
    "dailyBudget": 50,
    "startDate": "2025-01-20T00:00:00.000Z",
    "endDate": "2025-01-25T23:59:59.000Z",
    "createdAt": "2025-01-20T10:35:00.000Z",
    "ads": []
  }
}
```

---

## 3. 🎨 Create Ad Creative

### Request
```http
POST /api/ad-creatives
Content-Type: application/json
Authorization: Bearer {your_jwt_token}
```

### JSON Body
```json
{
  "contentId": "content-uuid-here",
  "adAccountId": "4005413613009650",
  "callToAction": "LEARN_MORE",
  "message": "Khám phá công nghệ mới nhất! 📱✨",
  "imageUrl": "https://example.com/images/tech-ad.jpg",
  "videoUrl": null,
  "linkUrl": "https://example.com/products/tech-gadget"
}
```

### Expected Response
```json
{
  "success": true,
  "message": "Ad creative created successfully",
  "data": {
    "id": "creative-uuid-here",
    "contentId": "content-uuid-here",
    "adAccountId": "4005413613009650",
    "creativeId": "23851261137850810",
    "callToAction": "LEARN_MORE",
    "createdAt": "2025-01-20T10:40:00.000Z"
  }
}
```

---

## 4. 📢 Create Ad

### Request
```http
POST /api/ads
Content-Type: application/json
Authorization: Bearer {your_jwt_token}
```

### JSON Body
```json
{
  "adSetId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "creativeId": "creative-uuid-here",
  "status": "PAUSED"
}
```

### Expected Response
```json
{
  "success": true,
  "message": "Ad created successfully",
  "data": {
    "id": "ad-uuid-here",
    "adSetId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "creativeId": "creative-uuid-here",
    "adId": "23851261137850820",
    "status": "PAUSED",
    "createdAt": "2025-01-20T10:45:00.000Z"
  }
}
```

---

## 5. 📈 Get Campaigns

### Request
```http
GET /api/ad-campaigns?brandId=c5b6337b-17ab-4842-82e7-a81a90b8c8aa&page=1&pageSize=20
Authorization: Bearer {your_jwt_token}
```

### Expected Response
```json
{
  "success": true,
  "message": "Campaigns retrieved successfully",
  "data": {
    "data": [
      {
        "id": "f1e2143c-2574-499b-86f5-f571af4cbc45",
        "userId": "c9f3fbe8-aca3-4101-bd9e-a5e730dfcbf5",
        "brandId": "c5b6337b-17ab-4842-82e7-a81a90b8c8aa",
        "adAccountId": "4005413613009650",
        "facebookCampaignId": "23851261137850792",
        "name": "Test Campaign - Video Views",
        "objective": "VIDEO_VIEWS",
        "budget": 100,
        "startDate": "2025-01-20T00:00:00.000Z",
        "endDate": "2025-01-25T23:59:59.000Z",
        "isActive": true,
        "createdAt": "2025-01-20T10:30:00.000Z",
        "updatedAt": "2025-01-20T10:30:00.000Z",
        "adSets": []
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20
  }
}
```

---

## 6. 📊 Get Ad Sets by Campaign

### Request
```http
GET /api/ad-sets/campaign/f1e2143c-2574-499b-86f5-f571af4cbc45
Authorization: Bearer {your_jwt_token}
```

### Expected Response
```json
{
  "success": true,
  "message": "Ad sets retrieved successfully",
  "data": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "campaignId": "f1e2143c-2574-499b-86f5-f571af4cbc45",
      "facebookAdSetId": "23851261137850801",
      "name": "Test Ad Set - Vietnam Targeting",
      "targeting": "{\"geo_locations\":{\"countries\":[\"VN\"]},\"age_min\":18,\"age_max\":35,\"genders\":[\"MALE\",\"FEMALE\"]}",
      "dailyBudget": 50,
      "startDate": "2025-01-20T00:00:00.000Z",
      "endDate": "2025-01-25T23:59:59.000Z",
      "createdAt": "2025-01-20T10:35:00.000Z",
      "ads": []
    }
  ]
}
```

---

## 7. 🗑️ Delete Campaign

### Request
```http
DELETE /api/ad-campaigns/f1e2143c-2574-499b-86f5-f571af4cbc45
Authorization: Bearer {your_jwt_token}
```

### Expected Response
```json
{
  "success": true,
  "message": "Campaign deleted successfully",
  "data": null
}
```

---

## 8. 🗑️ Delete Ad Set

### Request
```http
DELETE /api/ad-sets/a1b2c3d4-e5f6-7890-abcd-ef1234567890
Authorization: Bearer {your_jwt_token}
```

### Expected Response
```json
{
  "success": true,
  "message": "Ad set deleted successfully",
  "data": null
}
```

---

## 🔧 Sandbox vs Production Mode

### Sandbox Mode (VND Currency)
```json
{
  "budget": 100,        // 100 VND
  "dailyBudget": 50     // 50 VND
}
```

### Production Mode (USD Currency)
```json
{
  "budget": 100,        // $100 USD = 10000 cents
  "dailyBudget": 50     // $50 USD = 5000 cents
}
```

---

## 📝 Targeting Examples

### Basic Targeting (Vietnam)
```json
{
  "geo_locations": {
    "countries": ["VN"]
  },
  "age_min": 18,
  "age_max": 35,
  "genders": ["MALE", "FEMALE"]
}
```

### Advanced Targeting with Interests
```json
{
  "geo_locations": {
    "countries": ["VN"],
    "cities": [
      {
        "key": "Ho_Chi_Minh_City",
        "name": "Ho Chi Minh City"
      },
      {
        "key": "Hanoi",
        "name": "Hanoi"
      }
    ]
  },
  "age_min": 18,
  "age_max": 45,
  "genders": ["MALE", "FEMALE"],
  "interests": [
    {
      "id": "6003107902433",
      "name": "Technology"
    },
    {
      "id": "6004037226461",
      "name": "Mobile phones"
    },
    {
      "id": "6003348634581",
      "name": "Smartphones"
    }
  ],
  "behaviors": [
    {
      "id": "6002714895372",
      "name": "Small business owners"
    }
  ]
}
```

### Lookalike Audience Targeting
```json
{
  "geo_locations": {
    "countries": ["VN"]
  },
  "age_min": 18,
  "age_max": 65,
  "genders": ["MALE", "FEMALE"],
  "custom_audiences": [
    {
      "id": "23851261137850830",
      "name": "My Custom Audience"
    }
  ],
  "lookalike_audiences": [
    {
      "id": "23851261137850831",
      "name": "Lookalike 1% Vietnam"
    }
  ]
}
```

---

## 🚨 Error Examples

### Invalid Campaign ID
```json
{
  "success": false,
  "message": "Campaign not found",
  "data": null
}
```

### Invalid Targeting JSON
```json
{
  "success": false,
  "message": "Invalid targeting JSON format",
  "data": null
}
```

### Facebook API Error
```json
{
  "success": false,
  "message": "Facebook API Error: {\"error\":{\"message\":\"Invalid parameter\",\"type\":\"OAuthException\",\"code\":100}}",
  "data": null
}
```

---

## 🔑 Authentication

### JWT Token Format
```
Authorization: Bearer eyJhbGciOiJFUzI1NiIsImtpZCI6IjVlYzIzYzdhLWZhMTMtNGU4NC05MTAyLTgyZmViMDM3MTA5ZSIsInR5cCI6IkpXVCJ9...
```

### Required Permissions
- `ads_management` - Manage ads
- `ads_read` - Read ads data
- `business_management` - Manage business assets

---

## 📊 Testing Checklist

- [ ] Create Campaign with VND budget (Sandbox)
- [ ] Create Campaign with USD budget (Production)
- [ ] Create Ad Set with targeting
- [ ] Create Ad Creative with image
- [ ] Create Ad Creative with video
- [ ] Create Ad
- [ ] Get Campaigns list
- [ ] Get Ad Sets by Campaign
- [ ] Delete Ad Set
- [ ] Delete Campaign
- [ ] Test error handling
- [ ] Test validation

---

## 🎯 Notes

1. **Sandbox Mode**: Sử dụng VND currency, budget không cần convert
2. **Production Mode**: Sử dụng USD currency, budget convert to cents (x100)
3. **Facebook IDs**: Được lưu trong database để sync với Facebook
4. **Error Handling**: Graceful fallback khi Facebook API fail
5. **Logging**: Chi tiết cho debugging và monitoring
