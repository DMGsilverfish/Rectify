using Microsoft.AspNetCore.Identity;

namespace Rectify.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string CompanyName { get; set; }

        public bool? Reports { get; set; }
        public byte[]? OwnerImage { get; set; }
        public byte[]? LogoImage { get; set; }

        public string? City { get; set; }
        public string? BranchAddress { get; set; }
        public string? PreferredContact { get; set; }

        public string FullName { get; set; }

    }
}
