namespace InvestLab.Data.Interfaces
{
    public interface IUserSettingRepository
    {
        Task AddAsync(UserSetting setting);
    }
}
