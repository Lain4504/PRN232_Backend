using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AISAM.Data.Model2
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

        [MaxLength(20)]
        [Column("repeat_type")]
        public string RepeatType { get; set; } = "none";

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ContentId")]
        public virtual Content Content { get; set; } = null!;
    }
}
