using InvestLab.Models.Documents;

namespace InvestLab.Data.Interfaces
{
    public interface IPortfolioHistoryRepository
    {
        Task InsertAsync(PortfolioHistory history);

        Task<List<PortfolioHistory>> GetByPortfolioAndDateAsync(int portfolioId, DateTime fromDate);

        Task<PortfolioHistory?> GetLatestAsync(int portfolioId);

        Task<PortfolioHistory?> GetPreviousAsync(int portfolioId);

        Task<bool> ExistsByDateAsync(int portfolioId, DateTime date);

        Task<PortfolioHistory?> GetOnOrBeforeAsync(int portfolioId, DateTime date);

        Task DeleteByPortfolioIdAsync(int portfolioId);

        /// <summary>
        /// Completa el campo "portfolioId" en documentos históricos preexistentes que aún
        /// no lo poseen, utilizando el portfolio correspondiente a cada usuario.
        /// Operación idempotente: solo afecta documentos sin "portfolioId".
        /// </summary>
        /// <param name="portfolioIdByUserId">Mapa de UserId a PortfolioId a aplicar.</param>
        Task BackfillPortfolioIdsAsync(IReadOnlyDictionary<int, int> portfolioIdByUserId);
    }
}
