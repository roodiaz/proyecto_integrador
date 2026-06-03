using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using InvestLab.Models.DTOs.Transaction;
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

        public async Task<(List<TransactionDto> Data, int Total)> SearchAsync(int userId, TransactionFilterDto filter)
        {
            var query = _context.Transactions
                .AsNoTracking()
                .Include(x => x.Asset)
                .Where(x => x.UserId == userId);

            if (!string.IsNullOrWhiteSpace(filter.Symbol))
                query = query.Where(x => EF.Functions.ILike(x.Asset.Symbol, $"%{filter.Symbol}%"));
            if (filter.Type.HasValue)
                query = query.Where(x => x.Type == filter.Type.Value);

            if (filter.Days.HasValue)
            {
                var fromDate = DateTime.UtcNow.AddDays(-filter.Days.Value);
                query = query.Where(x => x.CreatedAt >= fromDate);
            }

            query = filter.OrderBy?.ToLower() switch
            {
                "symbol" => query.OrderBy(x => x.Asset.Symbol),
                "amount" => query.OrderByDescending(x => x.Total),
                _ => query.OrderByDescending(x => x.CreatedAt)
            };

            var total = await query.CountAsync();

            var data = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new TransactionDto
                {
                    OperationDate = x.CreatedAt,
                    Symbol = x.Asset.Symbol,
                    Type = x.Type,
                    Quantity = x.Quantity,
                    BuyPrice = x.Price,
                    Total = x.Total
                })
                .ToListAsync();

            return (data, total);
        }
    }
}
