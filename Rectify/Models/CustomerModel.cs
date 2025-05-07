using System.ComponentModel.DataAnnotations;

namespace Rectify.Models
{
    public class CustomerModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string CustomerName { get; set; }
        [Required]
        public required string CustomerEmail { get; set; }
        [Required]
        public required string CustomerPhone { get; set; }
        [Required]
        public required string Message { get; set; }
        [Required]
        public required string TicketID { get; set; }



    }
}
