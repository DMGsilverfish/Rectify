using System.ComponentModel.DataAnnotations;

namespace Rectify.Models.ViewModels
{
    public class RegisterNextViewModel
    {
        [Required(ErrorMessage = "Select your preferred method of contact.")]
        [Display(Name = "Preferred Contact Method")]
        public string PrefferedContact { get; set; }
        [Required(ErrorMessage = "Select your companys city.")]
        [Display(Name = "City")]
        public string City { get; set; }
        [Required(ErrorMessage = "Select your companys brancg or address.")]
        [Display(Name = "Branch or Address")]
        public string BranchAddress { get; set; }

        [Display(Name = "Logo Image")]
        public IFormFile LogoImageFile { get; set; }

        [Display(Name = "Owner Image")]
        public IFormFile OwnerImageFile { get; set; }

        [Display(Name = "Receive Reports")]
        public bool Reports {  get; set; }

    }
}
