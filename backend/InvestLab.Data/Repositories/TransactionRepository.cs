using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using InvestLab.Models.DTOs.Transaction;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories
{
    internal class TransactionRepository : ITransactionRepository
    {
        private readonly InvestLabDbContext _context;
        /// <summary>
        /// Inicializa una nueva instancia del repositorio de transacciones.
        /// </summary>
        /// <param name="context">Contexto de base de datos de InvestLab.</param>
        public TransactionRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Agrega una nueva transacción al contexto para su posterior persistencia.
        /// </summary>
        /// <param name="transaction">Transacción a insertar.</param>
        public async Task InsertAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
        }

        /// <summary>
        /// Obtiene las últimas transacciones de un usuario, incluyendo el activo asociado, ordenadas por fecha de creación descendente.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <param name="take">Cantidad máxima de transacciones a obtener.</param>
        /// <returns>Lista de las transacciones más recientes del usuario.</returns>
        public async Task<List<Transaction>> GetLatestByUserAsync(int userId, int take)
        {
            return await _context.Transactions
                .Include(x => x.Asset)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Busca transacciones de un usuario aplicando filtros de símbolo, tipo, rango de días y orden, devolviendo los resultados paginados.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <param name="filter">Criterios de filtrado, paginación y orden a aplicar sobre la búsqueda.</param>
        /// <returns>Tupla con la lista de transacciones filtradas (proyectadas a DTO) y el total de registros que cumplen el filtro.</returns>
        public async Task<(List<TransactionDto> Data, int Total)> SearchAsync(int userId, TransactionFilterDto filter)
        {
            var query = _context.Transactions
                .AsNoTracking()
                .Include(x => x.Asset)
                .Where(x => x.UserId == userId);

            if (!string.IsNullOrWhiteSpace(filter.Symbol))
                query = query.Where(x => EF.Functions.ILike(x.Asset.Symbol, $"%{filter.Symbol}%"));
            if (filter.Type.HasValue)
                query = query.Where(x => x.Type == filter.Type.Value);

            if (filter.Days.HasValue)
            {
                var fromDate = DateTime.UtcNow.AddDays(-filter.Days.Value);
                query = query.Where(x => x.CreatedAt >= fromDate);
            }

            query = filter.OrderBy?.ToLower() switch
            {
                "symbol" => query.OrderBy(x => x.Asset.Symbol),
                "amount" => query.OrderByDescending(x => x.Total),
                _ => query.OrderByDescending(x => x.CreatedAt)
            };

            var total = await query.CountAsync();

            var data = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new TransactionDto
                {
                    OperationDate = x.CreatedAt,
                    Symbol = x.Asset.Symbol,
                    Type = x.Type,
                    Quantity = x.Quantity,
                    BuyPrice = x.Price,
                    Total = x.Total
                })
                .ToListAsync();

            return (data, total);
        }

        /// <summary>
        /// Elimina todas las transacciones asociadas a un usuario.
        /// </summary>
        /// <param name="userId">Identificador del usuario cuyas transacciones se eliminarán.</param>
        public async Task DeleteByUserIdAsync(int userId)
        {
            await _context.Transactions
                .Where(x => x.UserId == userId)
                .ExecuteDeleteAsync();
        }
    }
}
