namespace GalaxyBookWeb.Models
{
    public class EmployeeViewModel
    {
        public int? Id { get; set; }
        public string? EnglishName { get; set; }
        public string? GujaratiName { get; set; }
        public string? EntryType { get; set; } // 'પેલ' or 'મથાળા'
        public string? Active { get; set; } // 'Yes' or 'No'
    }
}