using InvestLab.Business.Interfaces;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Data.Repositories;
using InvestLab.Integrations.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace InvestLab.Business.Services
{
    internal class MarketPriceService : IMarketPriceService
    {
        private readonly ILogger<MarketPriceService> _logger;
        private readonly IPriceHistoryRepository _priceHistoryRepository;
        private readonly IExternalProvider _externalProvider;

        public MarketPriceService(ILogger<MarketPriceService> logger, IPriceHistoryRepository priceHistoryRepository, IExternalProvider externalProvider)
        {
            _priceHistoryRepository = priceHistoryRepository;
            _logger = logger;
            _externalProvider = externalProvider;
        }

        public async Task<Dictionary<string, decimal>> GetHistoricalPricesAsync(List<string> symbols)
        {
            // Obtiene los últimos precios históricos almacenados en Mongo
            var latestPrices = await _priceHistoryRepository.GetLatestPricesAsync();

            // Se queda únicamente con los símbolos solicitados
            var prices = latestPrices
                .Where(x => symbols.Contains(x.Symbol))
                .ToDictionary(x => x.Symbol, x => x.Close);

            // Detecta qué símbolos no tienen precio disponible en Mongo
            var missingSymbols = symbols
                .Where(x => !prices.ContainsKey(x))
                .ToList();

            // Para los símbolos faltantes consulta el proveedor externo
            foreach (var symbol in missingSymbols)
            {
                var market = await _externalProvider.GetPriceAsync(symbol);

                if (market != null)
                    prices[symbol] = market.Price;
            }

            // Devuelve un diccionario Symbol -> Price
            return prices;
        }
    }
}
