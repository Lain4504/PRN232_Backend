using System.ComponentModel.DataAnnotations;

namespace AISAM.Data.Model
{
	public class Product
	{
		[Key]
		public Guid Id { get; set; }

		public Guid BrandId { get; set; }

		[MaxLength(300)]
		[Required]
		public string Name { get; set; } = string.Empty;

		[MaxLength(100)]
		public string? Sku { get; set; }

		public string? Description { get; set; }

		public decimal? Price { get; set; }

		[MaxLength(10)]
		public string Currency { get; set; } = "USD";

		public string? Metadata { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

		// Navigation
		public virtual Brand Brand { get; set; } = null!;
	}
}


