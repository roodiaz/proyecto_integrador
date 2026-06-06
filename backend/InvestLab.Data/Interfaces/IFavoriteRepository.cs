using InvestLab.Models.DTOs.Favorite;

namespace InvestLab.Data.Interfaces;

public interface IFavoriteRepository
{
    Task<List<Favorite>> GetByUserAsync(int userId);

    Task<int> CountAsync(int userId);

    Task<bool> ExistsAsync(int userId, int assetId);

    Task AddAsync(Favorite favorite);

    Task<Favorite?> GetByUserAndAssetAsync(int userId, int assetId);

    void Remove(Favorite favorite);

    Task<(List<Favorite> data, int total)> GetPagedAsync(int userId, FavoriteFilterDto filter);

    Task DeleteByUserIdAsync(int userId);
}
