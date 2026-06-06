using System;
using System.Collections.Generic;
using System.Text;

namespace InvestLab.Business.Interfaces.Workers
{
    public interface IMarketPriceRefreshService
    {
        Task RefreshAsync();
    }
}
