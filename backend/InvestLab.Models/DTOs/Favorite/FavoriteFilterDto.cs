
namespace InvestLab.Models.DTOs.Favorite;

public class FavoriteFilterDto
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    // symbol | name | price | variationPercent
    public string? SortBy { get; set; }

    // asc | desc
    public string? SortDirection { get; set; }
}
