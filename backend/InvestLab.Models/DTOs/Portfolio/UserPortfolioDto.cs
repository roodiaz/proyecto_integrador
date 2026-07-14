namespace InvestLab.Models.DTOs.Portfolio;

/// <summary>
/// Representa un portfolio de un usuario para su uso en la pantalla de portfolios (tabs).
/// </summary>
public class UserPortfolioDto
{
    /// <summary>Identificador del portfolio.</summary>
    public int Id { get; set; }

    /// <summary>Nombre del portfolio.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Saldo virtual inicial con el que se creó el portfolio.</summary>
    public decimal InitialBalance { get; set; }

    /// <summary>Saldo disponible actual (efectivo, sin invertir).</summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>Indica si es el portfolio actualmente activo del usuario.</summary>
    public bool IsActive { get; set; }
}
