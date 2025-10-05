using System.ComponentModel.DataAnnotations;
using AISAM.Data.Enumeration;
using Microsoft.AspNetCore.Http;

namespace AISAM.Common.Dtos.Request
{
    public class CreateProfileRequest
    {
        [Required]
        public ProfileTypeEnum ProfileType { get; set; }

        [MaxLength(255)]
        public string? CompanyName { get; set; }

        [MaxLength(1000)]
        public string? Bio { get; set; }

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Avatar file to upload (alternative to AvatarUrl)
        /// </summary>
        public IFormFile? AvatarFile { get; set; }
    }
}