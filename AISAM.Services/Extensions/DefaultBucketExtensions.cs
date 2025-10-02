using AISAM.Data.Enumeration;

namespace AISAM.Services.Extensions
{
    public static class DefaultBucketExtensions
    {
        public static string GetName(this DefaultBucketEnum bucket)
        {
            return bucket switch
            {
                DefaultBucketEnum.Avatar => "avatar",
                DefaultBucketEnum.Image => "image",
                DefaultBucketEnum.Video => "video",
                _ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, null)
            };
        }

        public static bool IsPublic(this DefaultBucketEnum bucket)
        {
            return bucket switch
            {
                DefaultBucketEnum.Avatar => true,
                DefaultBucketEnum.Image => true,
                DefaultBucketEnum.Video => false,
                _ => false
            };
        }
    }
}
