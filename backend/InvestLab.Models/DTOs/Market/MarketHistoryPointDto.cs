using System;
using System.Collections.Generic;
using System.Text;

namespace InvestLab.Models.DTOs.Market
{
    namespace InvestLab.Models.DTOs.Market
    {
        public class MarketHistoryPointDto
        {
            public DateTime Date { get; set; }
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }
            public long Volume { get; set; }
        }
    }
}
