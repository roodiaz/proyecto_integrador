namespace InvestLab.Data.Interfaces
{
    public interface IUserTempCredentialRepository
    {
        Task<List<UserTempCredential>> GetActiveByUserIdAsync(int userId);
        Task<UserTempCredential?> GetLatestAsync(int userId);
        Task AddAsync(UserTempCredential credential);
    }
}
