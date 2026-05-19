using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace InvestLab.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly InvestLabDbContext _context;

        public UnitOfWork(InvestLabDbContext context)
        {
            _context = context;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
