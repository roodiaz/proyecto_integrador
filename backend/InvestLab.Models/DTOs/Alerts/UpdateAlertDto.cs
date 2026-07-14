namespace InvestLab.Models.DTOs.Alerts
{
    /// <summary>
    /// Datos para actualizar una alerta existente.
    /// </summary>
    public class UpdateAlertDto : CreateAlertDto
    {
        /// <summary>Identificador de la alerta a actualizar.</summary>
        public int Id { get; set; }
    }
}
