using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rectify.Models
{
    public class CompanyModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public required string CompanyName { get; set; }
        [Required]
        public required string City { get; set; }
        [Required]
        public required string BranchAddress { get; set; }

        // Foreign key to AspNetUsers
        public string UserId { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        public byte[]? LogoImage { get; set; }

    }
}
