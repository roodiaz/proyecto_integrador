namespace InvestLab.Business.Interfaces.Workers
{
    public interface IDailySnapshotWorker
    {
        /// <summary>
        /// Genera el snapshot del día de cierre indicado (uso normal, un único día).
        /// Las operaciones realizadas después de marketCloseUtc quedan excluidas.
        /// </summary>
        Task GenerateDailyPortfolioSnapshotsAsync(DateTime marketCloseUtc);

        /// <summary>
        /// Garantiza que existan snapshots para TODOS los días bursátiles faltantes
        /// desde la creación de cada usuario (o desde su último snapshot) hasta lastCloseUtc.
        /// Es idempotente: omite días que ya tienen registro.
        /// Usa precios históricos de la base de datos para reconstruir valores pasados.
        /// </summary>
        Task RunHistoricalCatchUpAsync(DateTime lastCloseUtc);

        Task SaveDailyMarketHistoryAsync();
    }
}
