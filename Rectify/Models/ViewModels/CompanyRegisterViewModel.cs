using System.ComponentModel.DataAnnotations;

namespace Rectify.Models.ViewModels
{
    public class CompanyRegisterViewModel
    {

        // Step 1
        
        public string ? Name { get; set; }

        
        [Display(Name = "Company Name")]
        public string ? CompanyName { get; set; }

        
        [Display(Name = "WhatsApp Number")]
        [Phone]
        public string ? PhoneNumber { get; set; }

        
        [EmailAddress]
        public string ? Email { get; set; }

        
        [StringLength(40, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string ? Password { get; set; }

        
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }

        // Step 2
        
        [Display(Name = "Preferred Contact Method")]
        public string ? PrefferedContact { get; set; }

        
        [Display(Name = "City")]
        public string ? City { get; set; }

        
        [Display(Name = "Branch or Address")]
        public string ? BranchAddress { get; set; }

        [Display(Name = "Logo Image")]
        public IFormFile ? LogoImageFile { get; set; }

        [Display(Name = "Owner Image")]
        public IFormFile ? OwnerImageFile { get; set; }

        [Display(Name = "Receive Reports")]
        public bool Reports { get; set; } = false;
    }
}
