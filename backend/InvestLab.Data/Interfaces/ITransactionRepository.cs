namespace InvestLab.Data.Interfaces
{
    public interface ITransactionRepository
    {
        Task InsertAsync(Transaction transaction);

        Task<List<Transaction>> GetLatestByUserAsync(int userId, int take);
    }
}
