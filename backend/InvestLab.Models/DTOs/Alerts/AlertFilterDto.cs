namespace InvestLab.Models.DTOs.Alerts
{
    /// <summary>
    /// Parámetros de paginación y filtrado para listar alertas del usuario.
    /// </summary>
    public class AlertFilterDto
    {
        /// <summary>Filtro opcional por estado activa/pausada.</summary>
        public bool? IsActive { get; set; }

        /// <summary>Texto de búsqueda libre (por ejemplo, símbolo del activo).</summary>
        public string? Search { get; set; }

        /// <summary>Fecha de creación desde la cual incluir alertas.</summary>
        public DateTime? CreatedFrom { get; set; }

        /// <summary>Fecha de creación hasta la cual incluir alertas.</summary>
        public DateTime? CreatedTo { get; set; }

        /// <summary>Número de página (base 1).</summary>
        public int Page { get; set; } = 1;

        /// <summary>Cantidad de resultados por página.</summary>
        public int PageSize { get; set; } = 10;
    }
}
