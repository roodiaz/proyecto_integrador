using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

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

        public async Task<List<Transaction>> GetLatestByUserAsync(int userId, int take)
        {
            return await _context.Transactions
                .Include(x => x.Asset)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(take)
                .ToListAsync();
        }
    }
}
