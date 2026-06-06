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
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<UserService> _logger;

    // repositorios
    private readonly IUserRepository _userRepository;
    private readonly IUserSettingRepository _userSettingRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPortfolioHistoryRepository _portfolioHistoryRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserTempCredentialRepository _userTempCredentialRepository;

    public UserService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher, ILogger<UserService> logger, IUnitOfWork unitOfWork,
       IUserSettingRepository userSettingRepository, IPortfolioRepository portfolioRepository, ITransactionRepository transactionRepository,  IPortfolioHistoryRepository portfolioHistoryRepository, INotificationRepository notificationRepository, IAlertRepository alertRepository, IFavoriteRepository favoriteRepository, IRefreshTokenRepository refreshTokenRepository, IUserTempCredentialRepository userTempCredentialRepository)
    {
        _passwordHasher = passwordHasher;
        _logger = logger;

        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _userSettingRepository = userSettingRepository;
        _portfolioRepository = portfolioRepository;
        _transactionRepository = transactionRepository;
        _portfolioHistoryRepository = portfolioHistoryRepository;
        _notificationRepository = notificationRepository;
        _alertRepository = alertRepository;
        _favoriteRepository = favoriteRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _userTempCredentialRepository = userTempCredentialRepository;

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

            var folder = Path.Combine("wwwroot", "images");
            Directory.CreateDirectory(folder);

            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                var oldFileName = Path.GetFileName(user.ProfileImageUrl);
                var oldFilePath = Path.Combine(folder, oldFileName);

                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                }
            }

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
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

            return Response.Ok(user.ProfileImageUrl, "Imagen actualizada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UploadProfileImageAsync {UserId}", userId);
            return Response.Fail("Error interno del servidor");
        }
    }

    public async Task<Response> DeleteAccountAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("DeleteAccount: usuario no encontrado {UserId}", userId);
                return Response.Fail("Usuario no encontrado");
            }

            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                var oldFileName = Path.GetFileName(user.ProfileImageUrl);
                var oldFilePath = Path.Combine("wwwroot", "images", oldFileName);

                if (File.Exists(oldFilePath))
                    File.Delete(oldFilePath);
            }

            await _portfolioHistoryRepository.DeleteByUserIdAsync(userId);

            await _notificationRepository.DeleteByUserIdAsync(userId);
            await _alertRepository.DeleteByUserIdAsync(userId);
            await _favoriteRepository.DeleteByUserIdAsync(userId);
            await _transactionRepository.DeleteByUserIdAsync(userId);
            await _portfolioRepository.DeleteByUserIdAsync(userId);
            await _refreshTokenRepository.DeleteByUserIdAsync(userId);
            await _userTempCredentialRepository.DeleteByUserIdAsync(userId);
            await _userSettingRepository.DeleteByUserIdAsync(userId);

            await _userRepository.DeleteAsync(user);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Cuenta eliminada correctamente {UserId}", userId);

            return Response.Ok(null, "Cuenta eliminada correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en DeleteAccountAsync {UserId}", userId);
            return Response.Fail("Error interno del servidor");
        }
    }
}