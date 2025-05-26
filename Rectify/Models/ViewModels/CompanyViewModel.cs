using System.ComponentModel.DataAnnotations;

namespace Rectify.Models.ViewModels
{
    public class CompanyViewModel
    {
        [Required]
        public string CompanyName { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string BranchAddress { get; set; }


        public IFormFile? LogoImageFile { get; set; }

        public bool? Reports { get; set; }
    }


}
