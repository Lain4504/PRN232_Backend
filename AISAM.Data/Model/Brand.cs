using System.ComponentModel.DataAnnotations;

namespace AISAM.Data.Model
{
	public class Brand
	{
		[Key]
		public Guid Id { get; set; }

		public Guid? OrganizationId { get; set; }

		[MaxLength(200)]
		[Required]
		public string Name { get; set; } = string.Empty;

		public string? Description { get; set; }

		public Guid? LogoAssetId { get; set; }

		public string? Slogan { get; set; }

		public string? Usp { get; set; }

		// Store JSONB as text in EF; map to jsonb column in migrations
		public string? TargetAudience { get; set; }

		public string? BrandGuidelines { get; set; }

		public Guid? CreatedBy { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		// Navigation
		public virtual ICollection<Product> Products { get; set; } = new List<Product>();
	}
}


