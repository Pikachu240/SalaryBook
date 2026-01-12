using System.Collections.Generic;

namespace GalaxyBookWeb.Models
{
    // This represents the whole screen
    public class DailyEntryViewModel
    {
        public string EnglishName { get; set; }
        public string GujaratiName { get; set; }
        public string Date { get; set; }
        public string EntryType { get; set; }
        public int Uppad { get; set; }

        // This holds the rows of the grid
        public List<GridRow> Rows { get; set; } = new List<GridRow>();
    }

    // This represents one row in your grid (A, B, C...)
    public class GridRow
    {
        public int? Id { get; set; } // Null if new, Value if existing
        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
        public string D { get; set; }
        public string E { get; set; }
        public string F { get; set; }
        public string G { get; set; }
        public string Ct { get; set; }
    }

    // Simple helper for the Dropdown list
    public class EmployeeSelect
    {
        public string EnglishName { get; set; }
        public string GujaratiName { get; set; }
    }
}