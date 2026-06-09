namespace InvestLab.Models.DTOs.Dashboard
{
    public class DashboardPortfolioCompositionDto
    {
        public decimal AvailableBalance { get; set; }

        public decimal InvestedValue { get; set; }

        public decimal TotalValue { get; set; }

        public DateTime? EffectiveDate { get; set; }
    }
}
