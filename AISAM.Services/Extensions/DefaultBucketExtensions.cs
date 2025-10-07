﻿using AISAM.Data.Enumeration;

namespace AISAM.Services.Extensions
{
    public static class DefaultBucketExtensions
    {
        public static string GetName(this DefaultBucketEnum bucket)
        {
            return bucket switch
            {
                DefaultBucketEnum.Avatar => "avatar",
                DefaultBucketEnum.BrandAssets => "brandassets",
                DefaultBucketEnum.ProductMedia => "productmedia",
                DefaultBucketEnum.ContentMedia => "contentmedia",
                DefaultBucketEnum.AiGenerated => "aigenerated",
                DefaultBucketEnum.Misc => "misc",
                _ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, null)
            };
        }

        public static bool IsPublic(this DefaultBucketEnum bucket)
        {
            return bucket switch
            {
                DefaultBucketEnum.Avatar => true, // Public: Hiển thị profile
                DefaultBucketEnum.BrandAssets => true, // Public: Logo brand thường công khai
                DefaultBucketEnum.ProductMedia => true, // Public: Product images cho ads
                DefaultBucketEnum.ContentMedia => false, // Private: Content videos/images chưa approve
                DefaultBucketEnum.AiGenerated => false, // Private: AI generated để tránh leak
                DefaultBucketEnum.Misc => true, // media linh tinh lặt vặt, e.g.
                _ => false
            };
        }
    }
}
