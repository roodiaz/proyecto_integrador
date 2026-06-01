using InvestLab.Business.Interfaces.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Auth;
using InvestLab.Models.DTOs.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher, ILogger<UserService> logger, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Response> GetProfileAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdWithSettingsAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("GetProfile: usuario no encontrado {UserId}", userId);
                return Response.Fail("Usuario no encontrado");
            }

            return Response.Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Phone,
                user.BirthDate,
                user.ProfileImageUrl,
                settings = new
                {
                    user.UserSetting.Currency,
                    user.UserSetting.EmailNotifications
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetProfileAsync {UserId}", userId);
            return Response.Fail("Error interno del servidor");
        }
    }

    public async Task<Response> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        try
        {
            var user = await _userRepository.GetByIdWithSettingsAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("UpdateProfile: usuario no encontrado {UserId}", userId);
                return Response.Fail("Usuario no encontrado");
            }

            user.Username = dto.UserName;
            user.Phone = dto.Phone;
            user.BirthDate = dto.BirthDate;
            user.UpdateAt = DateTime.UtcNow;

            user.UserSetting.Currency = dto.Currency;
            user.UserSetting.EmailNotifications = dto.EmailNotifications;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Perfil actualizado {UserId}", userId);

            return Response.Ok(null, "Perfil actualizado correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UpdateProfileAsync {UserId}", userId);
            return Response.Fail("Error interno del servidor");
        }
    }

    public async Task<Response> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("ChangePassword: usuario no encontrado {UserId}", userId);
                return Response.Fail("Usuario no encontrado");
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);

            if (result == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("ChangePassword: password incorrecta {UserId}", userId);
                return Response.Fail("Contraseña actual incorrecta");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            user.PasswordChangedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Password actualizado {UserId}", userId);

            return Response.Ok(null, "Contraseña actualizada correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ChangePasswordAsync {UserId}", userId);
            return Response.Fail("Error interno del servidor");
        }
    }

    public async Task<Response> UploadProfileImageAsync(int userId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return Response.Fail("Archivo inválido");

            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("UploadImage: usuario no encontrado {UserId}", userId);
                return Response.Fail("Usuario no encontrado");
            }

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var folder = Path.Combine("wwwroot", "images");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            user.ProfileImageUrl = $"/images/{fileName}";
            user.UpdateAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Imagen actualizada {UserId}", userId);

            return Response.Ok(new { user.ProfileImageUrl }, "Imagen actualizada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UploadProfileImageAsync {UserId}", userId);
            return Response.Fail("Error interno del servidor");
        }
    }
}