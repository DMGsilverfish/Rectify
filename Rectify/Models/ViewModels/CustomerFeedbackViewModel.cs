using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Rectify.Models.ViewModels
{
    public class CustomerFeedbackViewModel
    {

        public int? SelectedCompanyId { get; set; }

        public string? CompanyName { get; set; }
        public string? CustomerName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Message { get; set; }

        public List<SelectListItem>? CompanyBranchOptions { get; set; }

        public string? LogoImageBase64 { get; set; }
        public string? OwnerImageBase64 { get; set; }

        public string? OwnerName { get; set; }

    }

}
