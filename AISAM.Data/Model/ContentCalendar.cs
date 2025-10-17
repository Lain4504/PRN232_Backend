using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AISAM.Data.Enumeration;

namespace AISAM.Data.Model
{
    [Table("content_calendar")]
    public class ContentCalendar
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("content_id")]
        public Guid ContentId { get; set; }

        [Required]
        [Column("scheduled_date")]
        public DateTime ScheduledDate { get; set; }

        [Column("scheduled_time")]
        public TimeSpan? ScheduledTime { get; set; }

        [MaxLength(50)]
        [Column("timezone")]
        public string Timezone { get; set; } = "UTC";

        [Column("repeat_type")]
        public RepeatTypeEnum RepeatType { get; set; } = RepeatTypeEnum.None;

        [Column("repeat_interval")]
        public int RepeatInterval { get; set; } = 1; // Every N days/weeks/months

        [Column("repeat_until")]
        public DateTime? RepeatUntil { get; set; } // Optional end date for recurring

        [Column("next_scheduled_date")]
        public DateTime? NextScheduledDate { get; set; } // For recurring schedules

        [Column("integration_ids")]
        public string? IntegrationIds { get; set; } // JSON array of selected integration IDs

        [Column("user_id")]
        public Guid UserId { get; set; } // User who created the schedule

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ContentId")]
        public virtual Content Content { get; set; } = null!;
    }
}
