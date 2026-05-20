namespace InvestLab.Data.Interfaces
{
    public interface ITransactionRepository
    {
        Task InsertAsync(Transaction transaction);
    }
}
