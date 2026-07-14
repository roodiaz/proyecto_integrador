namespace InvestLab.Models.DTOs.Portfolio
{
    /// <summary>
    /// Parámetros de paginación, filtrado y ordenamiento para listar posiciones abiertas.
    /// </summary>
    public class PortfolioOpenPositionsFilterDto
    {
        /// <summary>Número de página (base 1).</summary>
        public int Page { get; set; } = 1;

        /// <summary>Cantidad de resultados por página.</summary>
        public int PageSize { get; set; } = 10;

        /// <summary>Filtro opcional por símbolo del activo.</summary>
        public string? Symbol { get; set; }

        /// <summary>Campo de ordenamiento: symbol | sector | quantity | averagePrice | currentPrice | variationPercent | profitLoss.</summary>
        public string? SortBy { get; set; }

        /// <summary>Dirección del ordenamiento: asc | desc.</summary>
        public string? SortDirection { get; set; } = "asc";
    }
}
