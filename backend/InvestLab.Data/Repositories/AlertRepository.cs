using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using InvestLab.Models.DTOs.Alerts;
using Microsoft.EntityFrameworkCore;
using static InvestLab.Models.Enums;

namespace InvestLab.Data.Repositories
{
    public class AlertRepository : IAlertRepository
    {
        private readonly InvestLabDbContext _context;

        /// <summary>
        /// Inicializa una nueva instancia del repositorio de alertas.
        /// </summary>
        /// <param name="context">Contexto de base de datos de InvestLab.</param>
        public AlertRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Agrega una nueva alerta al contexto para su posterior persistencia.
        /// </summary>
        /// <param name="alert">Alerta a insertar.</param>
        public async Task AddAsync(Alert alert)
        {
            await _context.Alerts.AddAsync(alert);
        }

        /// <summary>
        /// Busca una alerta por su identificador, incluyendo el activo asociado.
        /// </summary>
        /// <param name="id">Identificador de la alerta.</param>
        /// <returns>La alerta encontrada con su activo asociado, o <c>null</c> si no existe.</returns>
        public async Task<Alert?> GetByIdAsync(int id)
        {
            return await _context.Alerts
                .Include(x => x.Asset)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <summary>
        /// Elimina una alerta del contexto.
        /// </summary>
        /// <param name="alert">Alerta a eliminar.</param>
        /// <returns>Una tarea completada que representa la operación de eliminación.</returns>
        public Task DeleteAsync(Alert alert)
        {
            _context.Alerts.Remove(alert);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cuenta la cantidad de alertas pertenecientes a un usuario.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <returns>Cantidad de alertas del usuario.</returns>
        public async Task<int> CountByUserAsync(int userId)
        {
            return await _context.Alerts.CountAsync(x => x.UserId == userId);
        }

        /// <summary>
        /// Verifica si ya existe una alerta con la misma combinación de usuario, activo, tipo de condición, operador y valor.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <param name="assetId">Identificador del activo.</param>
        /// <param name="type">Tipo de condición de la alerta.</param>
        /// <param name="op">Operador de comparación de la alerta.</param>
        /// <param name="value">Valor de comparación de la alerta.</param>
        /// <returns><c>true</c> si ya existe una alerta con esas características; en caso contrario, <c>false</c>.</returns>
        public async Task<bool> ExistsAsync(int userId, int assetId, ConditionType type, AlertOperator op, decimal value)
        {
            return await _context.Alerts.AnyAsync(x =>
                x.UserId == userId &&
                x.AssetId == assetId &&
                x.ConditionType == type &&
                x.Operator == op &&
                x.Value == value);
        }

        /// <summary>
        /// Obtiene las alertas de un usuario aplicando filtros de estado activo, búsqueda por símbolo y rango de fechas de creación, devolviendo los resultados paginados.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <param name="filter">Criterios de filtrado y paginación a aplicar sobre la búsqueda.</param>
        /// <returns>Tupla con la lista de alertas que cumplen el filtro y el total de registros encontrados.</returns>
        public async Task<(List<Alert>, int)> GetPagedAsync(int userId, AlertFilterDto filter)
        {
            var query = _context.Alerts
                .Include(a => a.Asset)
                .Where(a => a.UserId == userId);

            if (filter.IsActive.HasValue)
                query = query.Where(a => a.IsActive == filter.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToLower();
                query = query.Where(a =>a.Asset.Symbol.ToLower().Contains(search));
            }

            if (filter.CreatedFrom.HasValue)
            {
                var createdFrom = DateTime.SpecifyKind(
                    filter.CreatedFrom.Value.Date,
                    DateTimeKind.Utc);

                query = query.Where(a => a.CreatedAt >= createdFrom);
            }

            if (filter.CreatedTo.HasValue)
            {
                var createdTo = DateTime.SpecifyKind(
                    filter.CreatedTo.Value.Date.AddDays(1),
                    DateTimeKind.Utc);

                query = query.Where(a => a.CreatedAt < createdTo);
            }

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (data, total);
        }

        /// <summary>
        /// Calcula estadísticas de las alertas de un usuario: cantidad activas, pausadas, disparadas hoy y total.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <returns>Tupla con la cantidad de alertas activas, pausadas, disparadas hoy y el total de alertas.</returns>
        public async Task<(int, int, int, int)> GetStatsAsync(int userId)
        {
            var query = _context.Alerts.Where(x => x.UserId == userId);

            var active = await query.CountAsync(x => x.IsActive == true);
            var paused = await query.CountAsync(x => x.IsActive == false);

            var today = DateTime.UtcNow.Date;

            var triggeredToday = await query.CountAsync(x =>
                x.LastTriggered != null &&
                x.LastTriggered.Value.Date == today);

            var total = await query.CountAsync();

            return (active, paused, triggeredToday, total);
        }

        /// <summary>
        /// Obtiene todas las alertas activas del sistema, incluyendo el activo asociado a cada una.
        /// </summary>
        /// <returns>Lista de alertas activas con su activo asociado.</returns>
        public async Task<List<Alert>> GetActiveAlertsAsync()
        {
            return await _context.Alerts.Include(x => x.Asset)
                .Where(x => x.IsActive)
                .ToListAsync();
        }

        /// <summary>
        /// Marca una alerta como modificada en el contexto para su posterior actualización.
        /// </summary>
        /// <param name="alert">Alerta a actualizar.</param>
        /// <returns>Una tarea completada que representa la operación de actualización.</returns>
        public Task UpdateAsync(Alert alert)
        {
            _context.Alerts.Update(alert);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Obtiene las últimas alertas activas de un usuario, incluyendo el activo asociado, ordenadas por fecha de creación descendente.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <param name="take">Cantidad máxima de alertas a obtener.</param>
        /// <returns>Lista de las alertas activas más recientes del usuario.</returns>
        public async Task<List<Alert>> GetLatestActiveByUserAsync(int userId, int take)
        {
            return await _context.Alerts
                .Include(x => x.Asset)
                .Where(x => x.UserId == userId && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Elimina todas las alertas asociadas a un usuario.
        /// </summary>
        /// <param name="userId">Identificador del usuario cuyas alertas se eliminarán.</param>
        public async Task DeleteByUserIdAsync(int userId)
        {
            await _context.Alerts.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        }
    }
}

