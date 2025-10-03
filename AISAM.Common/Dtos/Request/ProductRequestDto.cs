namespace AISAM.Common.Dtos.Request
{
    public class ProductRequestDto
    {
        public Guid BrandId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? Price { get; set; }

        // Chỉ lưu danh sách URL ảnh đã upload lên Supabase
        public List<string>? ImageUrls { get; set; }
    }
}
