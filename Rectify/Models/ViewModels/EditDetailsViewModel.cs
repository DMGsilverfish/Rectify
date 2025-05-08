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
    }
}
