namespace AISAM.Common.Dtos.Response
{
    public class BrandResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? Slogan { get; set; }
        public string? Usp { get; set; }
        public string? TargetAudience { get; set; }
        public Guid? ProfileId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int ProductsCount { get; set; }
        public int ContentsCount { get; set; }
    }
}