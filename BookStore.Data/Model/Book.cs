using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookStore.Data.Model
{
    public class Book
    {
        [Key]
        [Required]
        public long Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Isbn { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Cover { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public float? Discount { get; set; }

        public int? Page { get; set; }

        public long? Price { get; set; }

        public DateOnly? PublicationDate { get; set; }

        [MaxLength(50)]
        public string? Size { get; set; }

        public int? Sold { get; set; }

        [MaxLength(50)]
        public string? State { get; set; }

        public int? Stock { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public int? Weight { get; set; }

        //[ForeignKey("Publisher")]
        //public long? PublisherId { get; set; }

        // Điều hướng
        //public virtual Publisher? Publisher { get; set; }

        //public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
        //public virtual ICollection<Image> Images { get; set; } = new List<Image>();
        //public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        //public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();

        [NotMapped]
        public long? SalePrice { get; set; }

        // Audit fields
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }

}
