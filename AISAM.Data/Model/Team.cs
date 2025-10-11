using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AISAM.Data.Enumeration;

namespace AISAM.Data.Model
{
    [Table("teams")]
    public class Team
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("vendor_id")]
        public Guid VendorId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        [Column("description")]
        public string? Description { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("status")]
        public TeamStatusEnum Status { get; set; } = TeamStatusEnum.Active;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("VendorId")]
        public virtual User? Vendor { get; set; }

        public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
        public virtual ICollection<TeamBrand> TeamBrands { get; set; } = new List<TeamBrand>();
    }
}
