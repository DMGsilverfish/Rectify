namespace Rectify.Models.ViewModels
{
    public class TicketStatsCard
    {
        public string Title { get; set; }
        public int Total { get; set; }
        public double? Average { get; set; } // null if only 1 company
        public double PercentChange { get; set; }
        public bool IsPositiveChange => PercentChange >= 0;
    }

    public class ReportsViewModel
    {
        public List<CompanyModel> Companies { get; set; } = new();
        public string UserId { get; set; }
        public CompanyModel Company { get; set; }
        public List<TicketStatsCard> StatCards { get; set; } = new();
    }

}
