using InvestLab.Models;
using InvestLab.Models.DTOs.Auth;
using InvestLab.Models.DTOs.User;
using Microsoft.AspNetCore.Http;

namespace InvestLab.Business.Interfaces.Api
{
    public interface IUserService
    {
        Task<Response> GetProfileAsync(int userId);

        Task<Response> UpdateProfileAsync(int userId, UpdateProfileDto dto);

        Task<Response> ChangePasswordAsync(int userId, ChangePasswordDto dto);

        Task<Response> RequestEmailChangeAsync(int userId, RequestEmailChangeDto dto);

        Task<Response> ConfirmEmailChangeAsync(int userId, ConfirmEmailChangeDto dto);

        Task<Response> UploadProfileImageAsync(int userId, IFormFile file);

        Task<Response> DeleteAccountAsync(int userId);
    }
}
