namespace InvestLab.Models.DTOs.Transaction
{
    /// <summary>
    /// Resultado paginado del historial de operaciones.
    /// </summary>
    public class TransactionSearchResponseDto
    {
        /// <summary>Operaciones de la página solicitada.</summary>
        public List<TransactionDto> Data { get; set; } = [];

        /// <summary>Cantidad total de operaciones que cumplen el filtro (sin paginar).</summary>
        public int Total { get; set; }
    }
}
