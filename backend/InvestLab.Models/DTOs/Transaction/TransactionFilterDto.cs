using static InvestLab.Models.Enums;

namespace InvestLab.Models.DTOs.Transaction
{
    /// <summary>
    /// Parámetros de paginación, filtrado y ordenamiento para el historial de operaciones.
    /// </summary>
    public class TransactionFilterDto
    {
        /// <summary>Número de página (base 1).</summary>
        public int Page { get; set; } = 1;

        /// <summary>Cantidad de resultados por página.</summary>
        public int PageSize { get; set; } = 10;

        /// <summary>Filtro opcional por símbolo del activo.</summary>
        public string? Symbol { get; set; }

        /// <summary>Filtro opcional por tipo de operación (compra o venta).</summary>
        public TransactionType? Type { get; set; }

        /// <summary>Fecha desde la cual incluir operaciones.</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Fecha hasta la cual incluir operaciones.</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>Campo de ordenamiento: date | symbol | sector | quantity | price | total.</summary>
        public string? SortBy { get; set; }

        /// <summary>Dirección del ordenamiento: asc | desc.</summary>
        public string? SortDirection { get; set; }
    }
}
