namespace Rectify.Models.ViewModels
{
    public class UpdateTicketStatusRequest
    {
        public int TicketId { get; set; }
        public string Status { get; set; }
    }
}
