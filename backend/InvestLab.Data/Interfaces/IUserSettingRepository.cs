namespace InvestLab.Data.Interfaces
{
    public interface IUserSettingRepository
    {
        Task AddAsync(UserSetting setting);
        Task<UserSetting?> GetByUserIdAsync(int userId);
        Task ResetDailyLimitsAsync();
    }
}
