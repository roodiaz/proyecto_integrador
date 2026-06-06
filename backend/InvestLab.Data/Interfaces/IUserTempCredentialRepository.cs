namespace InvestLab.Data.Interfaces
{
    public interface IUserTempCredentialRepository
    {
        Task<UserTempCredential> GetByUserIdAsync(int userId);

        Task AddAsync(UserTempCredential credential);

        Task DeleteByUserIdAsync(int userId);
    }
}
