namespace InvestLab.Models.DTOs.Portfolio;

/// <summary>
/// Representa un portfolio de un usuario para su uso en la pantalla de portfolios (tabs).
/// </summary>
public class UserPortfolioDto
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal InitialBalance { get; set; }

    public decimal CurrentBalance { get; set; }

    public bool IsActive { get; set; }
}
