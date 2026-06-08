using System;
using System.Collections.Generic;
using System.Text;

namespace InvestLab.Models
{
    public class Enums
    {
        public enum ConditionType
        {
            Price = 1,
            Percentage = 2
        }

        public enum AlertOperator
        {
            GreaterThan = 1,
            LessThan = 2,
            GreaterThanOrEqual = 3,
            LessThanOrEqual = 4,
            Equal = 5
        }

        public enum TransactionType
        {
            Buy = 1,
            Sell = 2
        }

        public enum MarketProviderType
        {
            Yahoo = 1,
            EodHistoricalData = 2
        }
    }
}
