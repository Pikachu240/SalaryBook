using System.Collections.Generic;

namespace GalaxyBookWeb.Models
{
    public class ReportViewModel
    {
        public string Name { get; set; }
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
        public int D { get; set; }
        public int E { get; set; }
        public int F { get; set; }
        public int G { get; set; }
        public decimal Ct { get; set; }
        public int TotalCount { get; set; }
        public decimal TotalKam { get; set; }
        public decimal Uppad { get; set; }
        public decimal Jama { get; set; }
    }

    public class ReportResponse
    {
        // We send the dynamic headers (Rates) to the UI so column names show "100", "200" etc.
        public decimal RateA { get; set; }
        public decimal RateB { get; set; }
        public decimal RateC { get; set; }
        public decimal RateD { get; set; }
        public decimal RateE { get; set; }
        public decimal RateF { get; set; }
        public decimal RateG { get; set; }

        public List<ReportViewModel> Rows { get; set; } = new List<ReportViewModel>();
    }
}