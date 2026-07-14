namespace InvestLab.Models.DTOs.Portfolio
{
    /// <summary>
    /// Precio actual de mercado de un activo.
    /// </summary>
    public class AssetPriceDto
    {
        /// <summary>Símbolo (ticker) del activo.</summary>
        public string Symbol { get; set; } = default!;

        /// <summary>Precio actual del activo.</summary>
        public decimal CurrentPrice { get; set; }
    }
}
