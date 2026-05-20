using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;

namespace InvestLab.Data.Repositories
{
    internal class TransactionRepository : ITransactionRepository
    {
        private readonly InvestLabDbContext _context;
        public TransactionRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        public async Task InsertAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
        }
    }
}
