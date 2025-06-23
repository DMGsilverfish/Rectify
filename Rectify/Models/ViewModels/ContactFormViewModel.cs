using System.ComponentModel.DataAnnotations;

namespace Rectify.Models.ViewModels
{
    public class ContactFormViewModel
    {
        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Message { get; set; }
    }
}
