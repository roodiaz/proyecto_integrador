using System;
using System.Collections.Generic;
using System.Text;

namespace InvestLab.Data.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task<User?> GetByIdWithSettingsAsync(int userId);
        Task<User?> GetByIdAsync(int userId);
        Task UpdateAsync(User user);
    }
}
