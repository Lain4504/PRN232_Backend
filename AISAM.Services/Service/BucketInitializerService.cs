using Microsoft.Extensions.Hosting;
using AISAM.Services.Enums;

namespace AISAM.Services.Service
{
    public class BucketInitializerService : IHostedService
    {
        private readonly Supabase.Client _supabase;

        public BucketInitializerService(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var existingBuckets = await _supabase.Storage.ListBuckets() ?? new List<Supabase.Storage.Bucket>();

            foreach (DefaultBucket bucket in Enum.GetValues(typeof(DefaultBucket)))
            {
                var name = bucket.GetName();
                var isPublic = bucket.IsPublic();

                if (!existingBuckets.Any(b => b.Id == name))
                {
                    await _supabase.Storage.CreateBucket(name);

                    if (isPublic)
                    {
                        await _supabase.Storage.UpdateBucket(
                            name,
                            new Supabase.Storage.BucketUpsertOptions { Public = true }
                        );
                    }

                    Console.WriteLine($"[Supabase] Created bucket: {name} (public={isPublic})");
                }
                else
                {
                    Console.WriteLine($"[Supabase] Bucket already exists: {name}");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
