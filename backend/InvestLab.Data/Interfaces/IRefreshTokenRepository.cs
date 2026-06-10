namespace InvestLab.Data.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);

        Task AddAsync(RefreshToken token);

        Task DeleteByUserIdAsync(int userId);

        Task RevokeAllByUserIdAsync(int userId);
    }
}
