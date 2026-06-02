namespace InvestLab.Models.DTOs.Alerts
{
    public class AlertFilterDto
    {
        public bool? IsActive { get; set; }

        public string? Search { get; set; }

        public DateTime? CreatedFrom { get; set; }

        public DateTime? CreatedTo { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
