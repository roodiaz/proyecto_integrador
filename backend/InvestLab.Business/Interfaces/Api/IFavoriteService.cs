using InvestLab.Models;
using InvestLab.Models.DTOs.Favorite;

namespace InvestLab.Business.Interfaces.Api;

public interface IFavoriteService
{
    Task<Response> GetAsync(int userId, FavoriteFilterDto filter);

    Task<Response> AddAsync(int userId, AddFavoriteDto dto);

    Task<Response> RemoveAsync(int userId, string symbol);

    Task<Response> GetCountAsync(int userId);
}
