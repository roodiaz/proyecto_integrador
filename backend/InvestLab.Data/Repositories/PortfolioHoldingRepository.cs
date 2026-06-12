using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories
{
    internal class PortfolioHoldingRepository : IPortfolioHoldingRepository
    {
        private readonly InvestLabDbContext _context;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="PortfolioHoldingRepository"/> con el contexto de base de datos especificado.
        /// </summary>
        /// <param name="context">Contexto de base de datos de InvestLab utilizado para realizar las operaciones.</param>
        public PortfolioHoldingRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene de forma asincrónica una posición que coincida con el portfolio y el activo indicados.
        /// </summary>
        /// <param name="portfolioId">Identificador del portfolio propietario de la posición.</param>
        /// <param name="assetId">Identificador del activo asociado a la posición.</param>
        /// <returns>Una tarea que representa la operación asincrónica, cuyo resultado contiene la posición encontrada o <c>null</c> si no existe.</returns>
        public async Task<PortfolioHolding?> GetByPortfolioAndAssetAsync(int portfolioId, int assetId)
        {
            return await _context.PortfolioHoldings.FirstOrDefaultAsync(x => x.PortfolioId == portfolioId && x.AssetId == assetId);
        }

        /// <summary>
        /// Agrega de forma asincrónica un nuevo registro de posición al contexto de base de datos.
        /// </summary>
        /// <param name="portfolioHolding">Entidad de posición a insertar.</param>
        /// <returns>Una tarea que representa la operación asincrónica de inserción.</returns>
        public async Task InsertAsync(PortfolioHolding portfolioHolding)
        {
            await _context.PortfolioHoldings.AddAsync(portfolioHolding);
        }

        /// <summary>
        /// Marca un registro de posición existente para ser actualizado en el contexto de base de datos.
        /// </summary>
        /// <param name="portfolioHolding">Entidad de posición con los datos actualizados.</param>
        /// <returns>Una tarea ya completada que representa la finalización de la operación.</returns>
        public Task UpdateAsync(PortfolioHolding portfolioHolding)
        {
            _context.PortfolioHoldings.Update(portfolioHolding);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Marca un registro de posición existente para ser eliminado del contexto de base de datos.
        /// </summary>
        /// <param name="portfolioHolding">Entidad de posición a eliminar.</param>
        /// <returns>Una tarea ya completada que representa la finalización de la operación.</returns>
        public Task DeleteAsync(PortfolioHolding portfolioHolding)
        {
            _context.PortfolioHoldings.Remove(portfolioHolding);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Obtiene de forma asincrónica todas las posiciones de un portfolio, incluyendo la información de sus activos asociados.
        /// </summary>
        /// <param name="portfolioId">Identificador del portfolio cuyas posiciones se desean obtener.</param>
        /// <returns>Una tarea que representa la operación asincrónica, cuyo resultado es la lista de posiciones del portfolio.</returns>
        public async Task<List<PortfolioHolding>> GetByPortfolioAsync(int portfolioId)
        {
            return await _context.PortfolioHoldings
                .Include(x => x.Asset)
                .Where(x => x.PortfolioId == portfolioId)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene de forma asincrónica las posiciones de un portfolio de manera paginada, incluyendo la información de sus activos asociados.
        /// </summary>
        /// <param name="portfolioId">Identificador del portfolio cuyas posiciones se desean obtener.</param>
        /// <returns>Una tarea que representa la operación asincrónica, cuyo resultado es la lista de posiciones del portfolio.</returns>
        public async Task<List<PortfolioHolding>> GetPagedByPortfolioAsync(int portfolioId)
        {
            return await _context.PortfolioHoldings
                .Include(x => x.Asset)
                .Where(x => x.PortfolioId == portfolioId)
                .ToListAsync();
        }

        /// <summary>
        /// Elimina de forma asincrónica todas las posiciones asociadas a un portfolio directamente en la base de datos.
        /// </summary>
        /// <param name="portfolioId">Identificador del portfolio cuyas posiciones se desean eliminar.</param>
        /// <returns>Una tarea que representa la operación asincrónica de eliminación.</returns>
        public async Task DeleteByPortfolioIdAsync(int portfolioId)
        {
            await _context.PortfolioHoldings
                .Where(x => x.PortfolioId == portfolioId)
                .ExecuteDeleteAsync();
        }
    }
}
