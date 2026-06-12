using InvestLab.Models.DTOs.Transaction;
using InvestLab.Models;

namespace InvestLab.Business.Interfaces.Api
{
    public interface ITransactionService
    {
         Task<Response> GetTransactionHistoryAsync(int userId, int portfolioId, TransactionFilterDto filter);

         Task<byte[]> ExportTransactionsToExcelAsync(int userId, int portfolioId, TransactionFilterDto filter);
    }
}
