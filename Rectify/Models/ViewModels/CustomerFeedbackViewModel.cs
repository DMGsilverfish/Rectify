using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Rectify.Models.ViewModels
{
    public class CustomerFeedbackViewModel
    {
        [Required(ErrorMessage = "Please select a company and branch.")]
        [Display(Name = "Company and Branch")]
        public int? SelectedCompanyId { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        public string CustomerName { get; set; }


        public List<SelectListItem>? CompanyBranchOptions { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Message is required.")]
        public string Message { get; set; }

        public string? LogoImageBase64 { get; set; }
        public string? OwnerImageBase64 { get; set; }

    }

}
