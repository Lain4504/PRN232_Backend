using AISAM.Data.Enumeration;
using System.ComponentModel.DataAnnotations;

namespace AISAM.Common.Dtos.Request
{
    public class TeamMemberCreateRequest
    {
        [Required]
        public Guid TeamId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public TeamMemberRoleEnum Role { get; set; }

        public List<string>? Permissions { get; set; }
    }

    public class TeamMemberUpdateRequest
    {
        public Guid? TeamId { get; set; } // cho phép đổi team
        public TeamMemberRoleEnum? Role { get; set; }
        public List<string>? Permissions { get; set; }
        public bool? IsActive { get; set; }
    }
}
