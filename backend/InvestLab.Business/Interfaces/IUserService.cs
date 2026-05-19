using InvestLab.Models;
using InvestLab.Models.DTOs.User.Request;
using Microsoft.AspNetCore.Http;

namespace InvestLab.Business.Interfaces
{
    public interface IUserService
    {
        Task<Response> GetProfileAsync(int userId);
        Task<Response> UpdateProfileAsync(int userId, UpdateProfileDto dto);
        Task<Response> ChangePasswordAsync(int userId, ChangePasswordDto dto);
        Task<Response> UploadProfileImageAsync(int userId, IFormFile file);
    }
}
