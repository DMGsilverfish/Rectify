using System.ComponentModel.DataAnnotations;

namespace Rectify.Models.ViewModels
{
    public class EditDetailsViewModel
    {
        
        public string UserId { get; set; }

        [Required]
        
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string City { get; set; }
        [Required]
        public string BranchAddress { get; set; }
        [Required]
        public string CompanyName { get; set; }

        [Required]
        public string PreferredContact { get; set; } // "Email" or "WhatsApp"

        public required bool Reports { get; set; } // Whether reports are enabled or not
    }
}
