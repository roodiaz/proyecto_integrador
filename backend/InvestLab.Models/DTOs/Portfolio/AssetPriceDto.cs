namespace InvestLab.Models.DTOs.Portfolio
{
    public class AssetPriceDto
    {
        public string Symbol { get; set; } = default!;

        public decimal CurrentPrice { get; set; }
    }
}
