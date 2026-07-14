namespace InvestLab.Models.DTOs.Portfolio
{
    /// <summary>
    /// Posición abierta de un activo dentro de un portfolio, con su rendimiento actual.
    /// </summary>
    public class PortfolioOpenPositionDto
    {
        /// <summary>Símbolo (ticker) del activo.</summary>
        public string Symbol { get; set; } = default!;

        /// <summary>Sector al que pertenece el activo.</summary>
        public string? Sector { get; set; }

        /// <summary>Cantidad de unidades en tenencia.</summary>
        public decimal Quantity { get; set; }

        /// <summary>Precio promedio de compra de la posición.</summary>
        public decimal AveragePrice { get; set; }

        /// <summary>Precio actual de mercado del activo.</summary>
        public decimal CurrentPrice { get; set; }

        /// <summary>Variación porcentual del precio respecto al cierre anterior.</summary>
        public decimal VariationPercent { get; set; }

        /// <summary>Ganancia o pérdida en valor absoluto de la posición.</summary>
        public decimal ProfitLoss { get; set; }

        /// <summary>Ganancia o pérdida de la posición expresada en porcentaje.</summary>
        public decimal ProfitLossPercent { get; set; }

        /// <summary>Indica si la posición sigue abierta.</summary>
        public bool IsOpen { get; set; }
    }
}
