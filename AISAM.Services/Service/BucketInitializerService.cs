using Microsoft.Extensions.Hosting;

public class BucketInitializerService : IHostedService
{
    private readonly Supabase.Client _supabase;

    // Danh sách bucket mặc định
    private readonly List<(string Name, bool IsPublic)> _defaultBuckets = new()
    {
        ("avatar", true),
        ("image", true),
        ("video", false)
    };

    public BucketInitializerService(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Sử dụng đầy đủ namespace Supabase.Storage.Bucket
        var existingBuckets = await _supabase.Storage.ListBuckets() ?? new List<Supabase.Storage.Bucket>();

        foreach (var (name, isPublic) in _defaultBuckets)
        {
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
