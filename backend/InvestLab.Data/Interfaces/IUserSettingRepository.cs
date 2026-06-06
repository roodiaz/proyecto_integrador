namespace InvestLab.Data.Interfaces
{
    public interface IUserSettingRepository
    {
        Task AddAsync(UserSetting setting);

        Task<UserSetting?> GetByUserIdAsync(int userId);

        Task ResetDailyLimitsAsync();

        Task ResetOperationsUsedTodayAsync(int userId);

        Task DeleteByUserIdAsync(int userId);
    }
}
