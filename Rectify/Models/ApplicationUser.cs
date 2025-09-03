using Microsoft.AspNetCore.Identity;

namespace Rectify.Models
{
    public class ApplicationUser : IdentityUser
    {

        public bool? Reports { get; set; }
        public byte[]? OwnerImage { get; set; }
        
        public string? PreferredContact { get; set; }

        public string FullName { get; set; }

        public string? Status { get; set; } = "Deactivated";

    }
}
