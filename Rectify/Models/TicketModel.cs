using Microsoft.AspNetCore.Razor.Language;
using System.ComponentModel.DataAnnotations;

namespace Rectify.Models
{
    public class TicketModel
    {
        [Key]
        public required string TicketID { get; set; }
        [Required]
        public required string CompanyID { get; set; }

        [Required]
        public DateTime DateOfMessage { get; set; }
        [Required]
        public required string Status {  get; set; }

        public DateTime DateLastUpdated { get; set; }

    }
}
