using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookStore.Common.Enumeration;

namespace BookStore.Data.Model
{
    public class Post
    {
        [Key]
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Brief { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public PostStatusEnum? Status { get; set; } = PostStatusEnum.Draft;

        [MaxLength(300)]
        public string? Thumbnail { get; set; } = string.Empty;

        public PostCategoryEnum? Category { get; set; } = PostCategoryEnum.News;

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? Author { get; set; }
    }
}