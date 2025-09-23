using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.API.DTO.Response
{
    public class PostResponseDto
    {
        public string Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Brief { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Thumbnail { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public UserResponseDto? Author { get; set; }    
    }
}