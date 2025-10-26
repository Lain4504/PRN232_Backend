# Facebook Ads API Test Samples

## Overview
Comprehensive test samples for the updated Facebook Ads APIs (AdSet, AdCreative, and Ads) with proper targeting and field formats based on Facebook Marketing API v24.0.

## Prerequisites
- Valid JWT token in Authorization header: `Bearer {your_jwt_token}`
- Existing campaign with Facebook Campaign ID
- Active social integration with valid Facebook access token
- Approved content for ad creative creation

---

## 1. Create Ad Set

### Endpoint
```
POST /api/ad-sets
```

### Sample Request
```json
{
  "campaignId": "f1e2143c-2574-499b-86f5-f571af4cbc45",
  "name": "Test Ad Set - Ho Chi Minh City",
  "targeting": "{\"geo_locations\":{\"countries\":[\"VN\"],\"cities\":[{\"key\":\"1566083493417727\",\"name\":\"Ho Chi Minh City\"}]},\"age_min\":18,\"age_max\":35,\"genders\":[1,2],\"interests\":[{\"id\":\"6003107902433\",\"name\":\"Technology\"},{\"id\":\"6004037226461\",\"name\":\"Mobile phones\"}]}",
  "dailyBudget": 50000,
  "startDate": "2025-01-27T00:00:00Z",
  "endDate": "2025-01-31T23:59:59Z"
}
```

### Targeting Examples

#### Basic Vietnam Targeting
```json
{
  "geo_locations": {
    "countries": ["VN"]
  },
  "age_min": 18,
  "age_max": 35,
  "genders": [1, 2]
}
```

#### Advanced Vietnam with Cities
```json
{
  "geo_locations": {
    "countries": ["VN"],
    "cities": [
      {
        "key": "1566083493417727",
        "name": "Ho Chi Minh City"
      },
      {
        "key": "1566083493417728", 
        "name": "Hanoi"
      }
    ]
  },
  "age_min": 18,
  "age_max": 45,
  "genders": [1, 2],
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
}
```

#### Lookalike Audience Targeting
```json
{
  "geo_locations": {
    "countries": ["VN"]
  },
  "age_min": 18,
  "age_max": 65,
  "genders": [1, 2],
  "lookalike_audiences": [
    {
      "id": "23851261137850831",
      "name": "Lookalike 1% Vietnam"
    }
  ]
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
    "name": "Test Ad Set - Ho Chi Minh City",
    "targeting": "{\"geo_locations\":{\"countries\":[\"VN\"],\"cities\":[{\"key\":\"1566083493417727\",\"name\":\"Ho Chi Minh City\"}]},\"age_min\":18,\"age_max\":35,\"genders\":[1,2]}",
    "dailyBudget": 50000,
    "startDate": "2025-01-27T00:00:00Z",
    "endDate": "2025-01-31T23:59:59Z",
    "createdAt": "2025-01-27T10:35:00Z",
    "ads": []
  }
}
```

---

## 2. Create Ad Creative

### Endpoint
```
POST /api/ad-creatives
```

### Sample Request (Image Creative)
```json
{
  "contentId": "content-uuid-here",
  "adAccountId": "act_1234567890",
  "callToAction": "LEARN_MORE"
}
```

### Sample Request (Video Creative)
```json
{
  "contentId": "content-uuid-with-video",
  "adAccountId": "act_1234567890", 
  "callToAction": "SHOP_NOW"
}
```

### Valid Call to Action Options
- `SHOP_NOW`
- `LEARN_MORE`
- `SIGN_UP`
- `DOWNLOAD`
- `BOOK_TRAVEL`
- `GET_QUOTE`

### Expected Response
```json
{
  "success": true,
  "message": "Ad creative created successfully",
  "data": {
    "id": "creative-uuid-here",
    "contentId": "content-uuid-here",
    "adAccountId": "act_1234567890",
    "creativeId": "23851261137850810",
    "callToAction": "LEARN_MORE",
    "createdAt": "2025-01-27T10:40:00Z",
    "contentPreview": {
      "title": "Amazing Product Launch",
      "textContent": "Discover our new innovative product that will change your life!",
      "imageUrl": "https://example.com/images/product.jpg",
      "videoUrl": null,
      "adType": "Image"
    }
  }
}
```

---

## 3. Create Ad

### Endpoint
```
POST /api/ads
```

### Sample Request
```json
{
  "adSetId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "creativeId": "creative-uuid-here",
  "status": "PAUSED"
}
```

### Valid Status Options
- `PAUSED` (recommended for initial creation)
- `ACTIVE` (will start running immediately)

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
    "createdAt": "2025-01-27T10:45:00Z"
  }
}
```

---

## 4. Update Ad Set Status

### Endpoint
```
PUT /api/ad-sets/{adSetId}/status
```

### Sample Request
```json
{
  "adSetId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "ACTIVE"
}
```

### Valid Status Options
- `ACTIVE`
- `PAUSED`

### Expected Response
```json
{
  "success": true,
  "message": "Ad set status updated successfully",
  "data": null
}
```

---

## 5. Update Ad Status

### Endpoint
```
PUT /api/ads/{adId}/status
```

### Sample Request
```json
{
  "adId": "ad-uuid-here",
  "status": "ACTIVE"
}
```

### Valid Status Options
- `ACTIVE`
- `PAUSED`

### Expected Response
```json
{
  "success": true,
  "message": "Ad status updated successfully",
  "data": null
}
```

---

## 6. Delete Ad Set

### Endpoint
```
DELETE /api/ad-sets/{adSetId}
```

### Sample Request
```
DELETE /api/ad-sets/a1b2c3d4-e5f6-7890-abcd-ef1234567890
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

## 7. Delete Ad

### Endpoint
```
DELETE /api/ads/{adId}
```

### Sample Request
```
DELETE /api/ads/ad-uuid-here
```

### Expected Response
```json
{
  "success": true,
  "message": "Ad deleted successfully",
  "data": null
}
```

---

## 8. Get Ad Sets by Campaign

### Endpoint
```
GET /api/ad-sets/campaign/{campaignId}
```

### Sample Request
```
GET /api/ad-sets/campaign/f1e2143c-2574-499b-86f5-f571af4cbc45
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
      "name": "Test Ad Set - Ho Chi Minh City",
      "targeting": "{\"geo_locations\":{\"countries\":[\"VN\"]}}",
      "dailyBudget": 50000,
      "startDate": "2025-01-27T00:00:00Z",
      "endDate": "2025-01-31T23:59:59Z",
      "createdAt": "2025-01-27T10:35:00Z",
      "ads": []
    }
  ]
}
```

---

## 9. Get Ad Performance Reports

### Endpoint
```
POST /api/ads/{adId}/pull-reports
```

### Sample Request
```
POST /api/ads/ad-uuid-here/pull-reports
```

### Expected Response
```json
{
  "success": true,
  "message": "Reports pulled successfully",
  "data": {
    "adId": "ad-uuid-here",
    "impressions": 1250,
    "clicks": 45,
    "ctr": 3.6,
    "spend": 25.50,
    "engagement": 12,
    "estimatedRevenue": 25.50,
    "reportDate": "2025-01-27T00:00:00Z",
    "rawData": "{\"data\":[{\"impressions\":\"1250\",\"clicks\":\"45\",\"ctr\":\"3.6\",\"spend\":\"25.50\"}]}"
  }
}
```

---

## Error Examples

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

### Missing Facebook Campaign ID
```json
{
  "success": false,
  "message": "Campaign does not have a Facebook Campaign ID. Please ensure the campaign was created successfully on Facebook.",
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

### Token Expired
```json
{
  "success": false,
  "message": "Facebook access token has expired. Please reconnect your account.",
  "data": null
}
```

---

## Testing Checklist

### Ad Set Flow
- [ ] Create Ad Set with basic targeting
- [ ] Create Ad Set with advanced targeting (cities + interests)
- [ ] Create Ad Set with lookalike audience
- [ ] Update Ad Set status (PAUSED → ACTIVE)
- [ ] Get Ad Sets by Campaign
- [ ] Delete Ad Set (with no active ads)

### Ad Creative Flow
- [ ] Create Ad Creative from image content
- [ ] Create Ad Creative from video content
- [ ] Test different call-to-action options
- [ ] Verify creative preview in response

### Ad Flow
- [ ] Create Ad with PAUSED status
- [ ] Create Ad with ACTIVE status
- [ ] Update Ad status (PAUSED → ACTIVE)
- [ ] Pull performance reports
- [ ] Delete Ad (PAUSED only)

### Error Handling
- [ ] Test with invalid campaign ID
- [ ] Test with invalid targeting JSON
- [ ] Test with expired Facebook token
- [ ] Test with missing Facebook IDs
- [ ] Test deletion of Ad Set with active ads

---

## Notes

1. **Sandbox vs Production**: 
   - Sandbox: VND currency, budget in VND units
   - Production: USD currency, budget in cents (multiply by 100)

2. **Facebook IDs**: All Facebook IDs are numeric strings, not GUIDs

3. **Status Flow**: 
   - Create as PAUSED → Review → Activate
   - Cannot delete ACTIVE ads/ad sets

4. **Targeting**: Use Facebook's targeting helper to get valid interest IDs and city keys

5. **Content Requirements**: 
   - Content must be approved before creating ad creative
   - Image/Video URLs must be publicly accessible

6. **Rate Limits**: Facebook has rate limits - add delays between requests if needed

7. **Validation**: All Facebook IDs are validated to ensure they're numeric strings before API calls
