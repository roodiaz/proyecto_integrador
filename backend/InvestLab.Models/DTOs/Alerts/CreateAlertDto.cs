using System.ComponentModel.DataAnnotations;

namespace InvestLab.Models.DTOs.Alerts
{
    public class CreateAlertDto
    {
        [Required]
        public string Symbol { get; set; }

        [Required]
        public string Condition { get; set; }

        public decimal? Price { get; set; }
        public decimal? PercentChange { get; set; }

        public bool IsActive { get; set; }
    }
}
