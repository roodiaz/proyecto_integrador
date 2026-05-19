using System;
using System.Collections.Generic;
using System.Text;

namespace InvestLab.Data.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdWithSettingsAsync(int userId);
        Task<User?> GetByIdAsync(int userId);
        Task UpdateAsync(User user);
    }
}
