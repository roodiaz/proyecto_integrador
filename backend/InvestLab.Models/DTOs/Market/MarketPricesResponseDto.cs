using System;
using System.Collections.Generic;
using System.Text;

namespace InvestLab.Models.DTOs.Market
{
    public class MarketPricesResponseDto
    {
        public List<MarketPriceDto> Prices { get; set; } = [];
        public DateTime? UpdatedAt { get; set; }
    }
}
