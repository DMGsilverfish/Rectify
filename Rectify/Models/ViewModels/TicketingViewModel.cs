
namespace Rectify.Models.ViewModels
{
    public class TicketingViewModel
    {
        public ApplicationUser User { get; set; }
        public List<TicketDisplayModel> Tickets { get; set; }

        public bool ShowCompanyInfo { get; set; }
    }

    public class TicketDisplayModel
    {
        public string TicketID { get; set; }
        public string CompanyName { get; set; }
        public string BranchAddress { get; set; }
        public DateTime DateOfMessage { get; set; }
        public string Status {  get; set; }
        public string Message { get; set; }

    }
}
