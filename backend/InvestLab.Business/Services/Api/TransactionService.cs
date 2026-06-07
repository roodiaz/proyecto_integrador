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

        /// <summary>
        /// Inicializa una nueva instancia del servicio de transacciones con sus dependencias.
        /// </summary>
        /// <param name="transactionRepository">Repositorio utilizado para acceder y consultar las transacciones.</param>
        /// <param name="unitOfWork">Unidad de trabajo para coordinar operaciones de persistencia.</param>
        /// <param name="logger">Registrador de eventos para el servicio de transacciones.</param>
        public TransactionService(ITransactionRepository transactionRepository, IUnitOfWork unitOfWork, ILogger<TransactionService> logger)
        {
            _transactionRepository = transactionRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el historial de transacciones de un usuario aplicando los filtros indicados.
        /// </summary>
        /// <param name="userId">Identificador del usuario cuyas transacciones se desean consultar.</param>
        /// <param name="filter">Criterios de filtrado y paginación a aplicar sobre la búsqueda de transacciones.</param>
        /// <returns>Una respuesta con los datos y el total de transacciones encontradas, o un mensaje de error si la operación falla.</returns>
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
