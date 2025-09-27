-- Sample data for testing AISAM application
-- Run these SQL commands to create test data

-- 1. Create a test user
INSERT INTO users (id, email, role, created_at)
VALUES (
    '11111111-1111-1111-1111-111111111111',
    'test@example.com',
    0, -- UserRoleEnum.User
    NOW()
);

-- 2. Create a test profile for the user
INSERT INTO profiles (id, user_id, profile_type, company_name, bio, avatar_url, is_deleted, created_at, updated_at)
VALUES (
    '22222222-2222-2222-2222-222222222222',
    '11111111-1111-1111-1111-111111111111',
    1, -- ProfileTypeEnum.Business
    'Test Company Ltd.',
    'This is a test company profile for development and testing purposes.',
    'https://via.placeholder.com/150x150/007bff/ffffff?text=TC',
    false,
    NOW(),
    NOW()
);

-- 3. Create a test brand
INSERT INTO brands (id, user_id, name, description, logo_url, slogan, usp, target_audience, profile_id, is_deleted, created_at, updated_at)
VALUES (
    '33333333-3333-3333-3333-333333333333',
    '11111111-1111-1111-1111-111111111111',
    'TechCorp Solutions',
    'Leading technology solutions provider specializing in AI and automation.',
    'https://via.placeholder.com/200x100/28a745/ffffff?text=TechCorp',
    'Innovation at Your Fingertips',
    'Cutting-edge AI solutions that transform businesses and drive growth through intelligent automation.',
    'Small to medium businesses looking to modernize their operations with AI technology.',
    '22222222-2222-2222-2222-222222222222',
    false,
    NOW(),
    NOW()
);

-- 4. Create another test user (Admin)
INSERT INTO users (id, email, role, created_at)
VALUES (
    '44444444-4444-4444-4444-444444444444',
    'admin@example.com',
    2, -- UserRoleEnum.Admin
    NOW()
);

-- 5. Create a personal profile for admin user
INSERT INTO profiles (id, user_id, profile_type, company_name, bio, avatar_url, is_deleted, created_at, updated_at)
VALUES (
    '55555555-5555-5555-5555-555555555555',
    '44444444-4444-4444-4444-444444444444',
    0, -- ProfileTypeEnum.Personal
    NULL,
    'System administrator with expertise in AI and social media management.',
    'https://via.placeholder.com/150x150/dc3545/ffffff?text=AD',
    false,
    NOW(),
    NOW()
);

-- 6. Create another test brand for variety
INSERT INTO brands (id, user_id, name, description, logo_url, slogan, usp, target_audience, profile_id, is_deleted, created_at, updated_at)
VALUES (
    '66666666-6666-6666-6666-666666666666',
    '11111111-1111-1111-1111-111111111111',
    'EcoFriendly Products',
    'Sustainable and eco-friendly product line committed to environmental responsibility.',
    'https://via.placeholder.com/200x100/20c997/ffffff?text=Eco',
    'Green Future, Today',
    '100% sustainable products that help reduce environmental impact while maintaining quality.',
    'Environmentally conscious consumers and businesses seeking sustainable alternatives.',
    '22222222-2222-2222-2222-222222222222',
    false,
    NOW(),
    NOW()
);

-- 7. Create test social accounts for the user
INSERT INTO social_accounts (id, user_id, platform, account_id, user_access_token, refresh_token, expires_at, is_active, is_deleted, created_at, updated_at)
VALUES 
-- Facebook account
(
    '77777777-7777-7777-7777-777777777777',
    '11111111-1111-1111-1111-111111111111',
    0, -- SocialPlatformEnum.Facebook
    'facebook_user_123456789',
    'EAABwzLixnjYBO1234567890abcdefghijklmnopqrstuvwxyz',
    NULL,
    NOW() + INTERVAL '60 days',
    true,
    false,
    NOW(),
    NOW()
),
-- Instagram account
(
    '88888888-8888-8888-8888-888888888888',
    '11111111-1111-1111-1111-111111111111',
    1, -- SocialPlatformEnum.Instagram
    'instagram_user_987654321',
    'IGQVJYeUp4YWNIY1h4OWZANeS1wRHZARdjJ5QmdueXN2R3NuWDl6bnU5ZAWxZA',
    NULL,
    NOW() + INTERVAL '60 days',
    true,
    false,
    NOW(),
    NOW()
),
-- TikTok account
(
    '99999999-9999-9999-9999-999999999999',
    '11111111-1111-1111-1111-111111111111',
    2, -- SocialPlatformEnum.TikTok
    'tiktok_user_456789123',
    'tiktok_access_token_abcdef123456789',
    'tiktok_refresh_token_xyz789456123',
    NOW() + INTERVAL '30 days',
    true,
    false,
    NOW(),
    NOW()
),
-- Twitter account
(
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    '11111111-1111-1111-1111-111111111111',
    3, -- SocialPlatformEnum.Twitter
    'twitter_user_789123456',
    'twitter_access_token_123456789abcdef',
    'twitter_refresh_token_987654321xyz',
    NOW() + INTERVAL '30 days',
    true,
    false,
    NOW(),
    NOW()
);

-- 8. Create social integrations linking brands to social accounts
INSERT INTO social_integrations (id, user_id, brand_id, social_account_id, platform, access_token, refresh_token, expires_at, external_id, is_active, is_deleted, created_at, updated_at)
VALUES 
-- TechCorp Solutions - Facebook Page integration
(
    'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    '11111111-1111-1111-1111-111111111111',
    '33333333-3333-3333-3333-333333333333', -- TechCorp Solutions brand
    '77777777-7777-7777-7777-777777777777', -- Facebook social account
    0, -- SocialPlatformEnum.Facebook
    'EAABwzLixnjYBO1234567890abcdefghijklmnopqrstuvwxyz_PAGE_TOKEN',
    NULL,
    NOW() + INTERVAL '60 days',
    'techcorp_page_123456789',
    true,
    false,
    NOW(),
    NOW()
),
-- TechCorp Solutions - Instagram Business integration
(
    'cccccccc-cccc-cccc-cccc-cccccccccccc',
    '11111111-1111-1111-1111-111111111111',
    '33333333-3333-3333-3333-333333333333', -- TechCorp Solutions brand
    '88888888-8888-8888-8888-888888888888', -- Instagram social account
    1, -- SocialPlatformEnum.Instagram
    'IGQVJYeUp4YWNIY1h4OWZANeS1wRHZARdjJ5QmdueXN2R3NuWDl6bnU5ZAWxZA_BUSINESS',
    NULL,
    NOW() + INTERVAL '60 days',
    'techcorp_instagram_987654321',
    true,
    false,
    NOW(),
    NOW()
),
-- EcoFriendly Products - TikTok integration
(
    'dddddddd-dddd-dddd-dddd-dddddddddddd',
    '11111111-1111-1111-1111-111111111111',
    '66666666-6666-6666-6666-666666666666', -- EcoFriendly Products brand
    '99999999-9999-9999-9999-999999999999', -- TikTok social account
    2, -- SocialPlatformEnum.TikTok
    'tiktok_access_token_abcdef123456789_BUSINESS',
    'tiktok_refresh_token_xyz789456123',
    NOW() + INTERVAL '30 days',
    'ecofriendly_tiktok_456789123',
    true,
    false,
    NOW(),
    NOW()
),
-- EcoFriendly Products - Twitter integration
(
    'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
    '11111111-1111-1111-1111-111111111111',
    '66666666-6666-6666-6666-666666666666', -- EcoFriendly Products brand
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', -- Twitter social account
    3, -- SocialPlatformEnum.Twitter
    'twitter_access_token_123456789abcdef_BUSINESS',
    'twitter_refresh_token_987654321xyz',
    NOW() + INTERVAL '30 days',
    'ecofriendly_twitter_789123456',
    true,
    false,
    NOW(),
    NOW()
);

-- Verification queries (optional - run these to check if data was inserted correctly)
-- SELECT * FROM users;
-- SELECT * FROM profiles;
-- SELECT * FROM brands;
-- SELECT * FROM social_accounts;
-- SELECT * FROM social_integrations;
