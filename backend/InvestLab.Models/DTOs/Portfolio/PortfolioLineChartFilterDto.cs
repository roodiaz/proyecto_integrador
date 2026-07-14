namespace InvestLab.Models.DTOs.Portfolio
{
    /// <summary>
    /// Filtro de período para la evolución histórica del portfolio.
    /// </summary>
    public class PortfolioLineChartFilterDto
    {
        /// <summary>Período del gráfico (por ejemplo 7d, 1m, 3m, 1y).</summary>
        public string Period { get; set; } = "7d";
    }
}
