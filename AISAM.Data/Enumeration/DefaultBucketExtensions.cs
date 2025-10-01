namespace AISAM.Services.Enums
{
    public static class DefaultBucketExtensions
    {
        public static string GetName(this DefaultBucket bucket)
        {
            return bucket switch
            {
                DefaultBucket.Avatar => "avatar",
                DefaultBucket.Image => "image",
                DefaultBucket.Video => "video",
                _ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, null)
            };
        }

        public static bool IsPublic(this DefaultBucket bucket)
        {
            return bucket switch
            {
                DefaultBucket.Avatar => true,
                DefaultBucket.Image => true,
                DefaultBucket.Video => false,
                _ => false
            };
        }
    }
}
