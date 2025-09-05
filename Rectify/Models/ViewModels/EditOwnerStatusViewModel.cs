namespace Rectify.Models.ViewModels
{
    public class EditOwnerStatusViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; } //display
        public string Email { get; set; } //display

        public string Status { get; set; }  // current status from DB
        public List<string> AvailableStatuses { get; set; } = new();
    }
}
