using System.ComponentModel.DataAnnotations;

namespace InvestLab.Models.DTOs.Alerts
{
    /// <summary>
    /// Datos para crear una alerta de precio sobre un activo.
    /// </summary>
    public class CreateAlertDto
    {
        /// <summary>Símbolo (ticker) del activo a monitorear.</summary>
        [Required]
        public string Symbol { get; set; }

        /// <summary>Condición que dispara la alerta (por ejemplo, precio mayor/menor o variación porcentual).</summary>
        [Required]
        public string Condition { get; set; }

        /// <summary>Precio umbral, requerido cuando la condición es por precio.</summary>
        public decimal? Price { get; set; }

        /// <summary>Variación porcentual umbral, requerida cuando la condición es por variación.</summary>
        public decimal? PercentChange { get; set; }

        /// <summary>Indica si la alerta se crea activa.</summary>
        public bool IsActive { get; set; }
    }
}
