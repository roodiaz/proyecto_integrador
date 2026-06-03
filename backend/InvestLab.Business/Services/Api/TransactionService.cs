using InvestLab.Business.Interfaces.Api;
using InvestLab.Data.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Transaction;
using Microsoft.Extensions.Logging;

namespace InvestLab.Business.Services.Api
{
    public class TransactionService : ITransactionService
    {
        private readonly ILogger<TransactionService> _logger;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public TransactionService(ITransactionRepository transactionRepository, IUnitOfWork unitOfWork, ILogger<TransactionService> logger)
        {
            _transactionRepository = transactionRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Response> GetTransactionHistoryAsync(int userId, TransactionFilterDto filter)
        {
            try
            {
                var result = await _transactionRepository.SearchAsync(userId, filter);

                return Response.Ok(
                    new TransactionSearchResponseDto
                    {
                        Data = result.Data,
                        Total = result.Total
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo transacciones para usuario {UserId}", userId);
                return Response.Fail("Error al obtener las transacciones");
            }
        }
    }
}
