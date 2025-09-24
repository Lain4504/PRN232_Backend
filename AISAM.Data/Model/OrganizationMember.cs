using System.ComponentModel.DataAnnotations;

namespace AISAM.Data.Model
{
	public class OrganizationMember
	{
		[Required]
		public Guid OrgId { get; set; }

		[Required]
		public Guid UserId { get; set; }

		[MaxLength(50)]
		public string? Role { get; set; }

		public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

		// Navigation
		public virtual Organization Organization { get; set; } = null!;
	}
}


