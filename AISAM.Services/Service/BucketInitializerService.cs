using AISAM.Data.Enumeration;
using AISAM.Services.Extensions;
using Microsoft.Extensions.Hosting;

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

            foreach (DefaultBucketEnum bucket in Enum.GetValues(typeof(DefaultBucketEnum)))
            {
                var name = bucket.GetName();
                var isPublic = bucket.IsPublic();

                if (!existingBuckets.Any(b => b.Id == name))
                {
                    await _supabase.Storage.CreateBucket(name);
                    Console.WriteLine($"[Supabase] Created bucket: {name}");
                }
                else
                {
                    Console.WriteLine($"[Supabase] Bucket already exists: {name}");
                }

                // Always update public status to match configuration
                try 
                {
                    await _supabase.Storage.UpdateBucket(
                        name,
                        new Supabase.Storage.BucketUpsertOptions { Public = isPublic }
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Supabase] Warning: Failed to update bucket {name}: {ex.Message}");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
