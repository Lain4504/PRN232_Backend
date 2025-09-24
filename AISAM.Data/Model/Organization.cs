using System.ComponentModel.DataAnnotations;

namespace AISAM.Data.Model
{
	public class Organization
	{
		[Key]
		public Guid Id { get; set; }

		[MaxLength(200)]
		[Required]
		public string Name { get; set; } = string.Empty;

		public Guid? OwnerUserId { get; set; }

		// Store JSON as text; database column should be jsonb in PostgreSQL
		public string? BillingInfo { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		// Navigation
		public virtual ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
		public virtual ICollection<Brand> Brands { get; set; } = new List<Brand>();
	}
}


