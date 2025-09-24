using System.ComponentModel.DataAnnotations;

namespace AISAM.Data.Model
{
	public class Schedule
	{
		[Key]
		public Guid Id { get; set; }

		public Guid? AdVariantId { get; set; }

		[Required]
		public DateTime ScheduledAt { get; set; }

		public string? PublishWindow { get; set; }

		public Guid? ApprovedBy { get; set; }

		public DateTime? ApprovedAt { get; set; }

		[MaxLength(50)]
		public string Status { get; set; } = "scheduled";
	}
}


