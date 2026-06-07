using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using InvestLab.Models.Documents;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories
{
    internal class PortfolioRepository : IPortfolioRepository
    {
        private readonly InvestLabDbContext _context;
        /// <summary>
        /// Inicializa una nueva instancia de <see cref="PortfolioRepository"/> con el contexto de base de datos especificado.
        /// </summary>
        /// <param name="context">Contexto de base de datos de InvestLab utilizado para realizar las operaciones.</param>
        public PortfolioRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene de forma asincrónica un registro de portafolio que coincida con el usuario y el activo indicados.
        /// </summary>
        /// <param name="userId">Identificador del usuario propietario del portafolio.</param>
        /// <param name="assetId">Identificador del activo asociado al portafolio.</param>
        /// <returns>Una tarea que representa la operación asincrónica, cuyo resultado contiene el portafolio encontrado o <c>null</c> si no existe.</returns>
        public async Task<Portfolio?> GetByUserAndAssetAsync(int userId, int assetId)
        {
            return await _context.Portfolios.FirstOrDefaultAsync(x => x.UserId == userId && x.AssetId == assetId);
        }

        /// <summary>
        /// Agrega de forma asincrónica un nuevo registro de portafolio al contexto de base de datos.
        /// </summary>
        /// <param name="portfolio">Entidad de portafolio a insertar.</param>
        /// <returns>Una tarea que representa la operación asincrónica de inserción.</returns>
        public async Task InsertAsync(Portfolio portfolio)
        {
            await _context.Portfolios.AddAsync(portfolio);
        }

        /// <summary>
        /// Marca un registro de portafolio existente para ser actualizado en el contexto de base de datos.
        /// </summary>
        /// <param name="portfolio">Entidad de portafolio con los datos actualizados.</param>
        /// <returns>Una tarea ya completada que representa la finalización de la operación.</returns>
        public Task UpdateAsync(Portfolio portfolio)
        {
            _context.Portfolios.Update(portfolio);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Marca un registro de portafolio existente para ser eliminado del contexto de base de datos.
        /// </summary>
        /// <param name="portfolio">Entidad de portafolio a eliminar.</param>
        /// <returns>Una tarea ya completada que representa la finalización de la operación.</returns>
        public Task DeleteAsync(Portfolio portfolio)
        {
            _context.Portfolios.Remove(portfolio);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Obtiene de forma asincrónica todos los portafolios pertenecientes a un usuario, incluyendo la información de sus activos asociados.
        /// </summary>
        /// <param name="userId">Identificador del usuario cuyos portafolios se desean obtener.</param>
        /// <returns>Una tarea que representa la operación asincrónica, cuyo resultado es la lista de portafolios del usuario.</returns>
        public async Task<List<Portfolio>> GetByUserAsync(int userId)
        {
            return await _context.Portfolios
                .Include(x => x.Asset)
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene de forma asincrónica los portafolios de un usuario de manera paginada, incluyendo la información de sus activos asociados.
        /// </summary>
        /// <param name="userId">Identificador del usuario cuyos portafolios se desean obtener.</param>
        /// <returns>Una tarea que representa la operación asincrónica, cuyo resultado es la lista de portafolios del usuario.</returns>
        public async Task<List<Portfolio>> GetPagedByUserAsync(int userId)
        {
            return await _context.Portfolios
                .Include(x => x.Asset)
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        /// <summary>
        /// Elimina de forma asincrónica todos los portafolios asociados a un usuario directamente en la base de datos.
        /// </summary>
        /// <param name="userId">Identificador del usuario cuyos portafolios se desean eliminar.</param>
        /// <returns>Una tarea que representa la operación asincrónica de eliminación.</returns>
        public async Task DeleteByUserIdAsync(int userId)
        {
            await _context.Portfolios
                .Where(x => x.UserId == userId)
                .ExecuteDeleteAsync();
        }
    }
}
