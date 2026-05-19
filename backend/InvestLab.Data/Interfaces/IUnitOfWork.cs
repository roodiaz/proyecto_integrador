using System;
using System.Collections.Generic;
using System.Text;

namespace InvestLab.Data.Interfaces
{
    public interface IUnitOfWork
    {
        Task SaveChangesAsync();
    }
}
