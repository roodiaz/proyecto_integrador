namespace InvestLab.Models.DTOs.Portfolio
{
    /// <summary>
    /// Punto de la serie histórica del valor total del portfolio.
    /// </summary>
    public class PortfolioLineChartItemDto
    {
        /// <summary>Fecha del punto de la serie.</summary>
        public DateTime Date { get; set; }

        /// <summary>Valor total del portfolio en esa fecha.</summary>
        public decimal TotalValue { get; set; }
    }
}
