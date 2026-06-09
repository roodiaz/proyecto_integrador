using InvestLab.Business.Interfaces.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Auth;
using InvestLab.Models.DTOs.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static InvestLab.Models.MessageCodes;

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
    private readonly IVerificationCodeService _verificationCodeService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="UserService"/> con sus dependencias y repositorios.
    /// </summary>
    /// <param name="userRepository">Repositorio de usuarios.</param>
    /// <param name="passwordHasher">Servicio para generar y verificar hashes de contraseñas.</param>
    /// <param name="logger">Registrador de eventos del servicio.</param>
    /// <param name="unitOfWork">Unidad de trabajo para confirmar cambios en la base de datos.</param>
    /// <param name="userSettingRepository">Repositorio de configuraciones de usuario.</param>
    /// <param name="portfolioRepository">Repositorio de carteras de inversión.</param>
    /// <param name="transactionRepository">Repositorio de transacciones.</param>
    /// <param name="portfolioHistoryRepository">Repositorio del historial de carteras.</param>
    /// <param name="notificationRepository">Repositorio de notificaciones.</param>
    /// <param name="alertRepository">Repositorio de alertas.</param>
    /// <param name="favoriteRepository">Repositorio de favoritos.</param>
    /// <param name="refreshTokenRepository">Repositorio de tokens de actualización.</param>
    /// <param name="userTempCredentialRepository">Repositorio de credenciales temporales de usuario.</param>
    public UserService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher, ILogger<UserService> logger, IUnitOfWork unitOfWork,
       IUserSettingRepository userSettingRepository, IPortfolioRepository portfolioRepository, ITransactionRepository transactionRepository,  IPortfolioHistoryRepository portfolioHistoryRepository, INotificationRepository notificationRepository, IAlertRepository alertRepository, IFavoriteRepository favoriteRepository, IRefreshTokenRepository refreshTokenRepository, IUserTempCredentialRepository userTempCredentialRepository, IVerificationCodeService verificationCodeService)
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
        _verificationCodeService = verificationCodeService;

    }

    /// <summary>
    /// Obtiene el perfil del usuario junto con su configuración asociada.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <returns>Una respuesta con los datos del perfil del usuario o un mensaje de error si no se encuentra.</returns>
    public async Task<Response> GetProfileAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdWithSettingsAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("GetProfile: usuario no encontrado {UserId}", userId);
                return Response.Fail("Usuario no encontrado", USER_NOT_FOUND);
            }

            return Response.Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Phone,
                user.BirthDate,
                user.ProfileImageUrl,
                user.LastLoginAt,
                settings = new
                {
                    user.UserSetting.Currency,
                    user.UserSetting.EmailNotifications,
                    Theme = user.UserSetting.Theme ?? "Dark",
                    Language = user.UserSetting.Language ?? "es"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetProfileAsync {UserId}", userId);
            return Response.Fail("Error interno del servidor", INTERNAL_ERROR);
        }
    }

    /// <summary>
    /// Actualiza los datos del perfil y la configuración del usuario.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="dto">Datos actualizados del perfil del usuario.</param>
    /// <returns>Una respuesta indicando si la actualización fue exitosa o el motivo del error.</returns>
    public async Task<Response> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        try
        {
            var user = await _userRepository.GetByIdWithSettingsAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("UpdateProfile: usuario no encontrado {UserId}", userId);
                return Response.Fail("Usuario no encontrado", USER_NOT_FOUND);
            }

            user.Username = dto.UserName;
            user.Phone = dto.Phone;
            user.BirthDate = dto.BirthDate.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(dto.BirthDate.Value, DateTimeKind.Utc))
                : null;
            user.UpdateAt = DateTime.UtcNow;

            user.UserSetting.Currency = dto.Currency;
            user.UserSetting.EmailNotifications = dto.EmailNotifications;
            user.UserSetting.Theme = dto.Theme ?? "Dark";
            if (!string.IsNullOrWhiteSpace(dto.Language))
                user.UserSetting.Language = dto.Language;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Perfil actualizado {UserId}", userId);

            return Response.Ok(null, "Perfil actualizado correctamente", PROFILE_UPDATE_SUCCESS);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UpdateProfileAsync {UserId}", userId);
            return Response.Fail("Error interno del servidor", INTERNAL_ERROR);
        }
    }

    /// <summary>
    /// Cambia la contraseña del usuario, verificando previamente la contraseña actual.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="dto">Datos con la contraseña actual y la nueva contraseña.</param>
    /// <returns>Una respuesta indicando si el cambio de contraseña fue exitoso o el motivo del error.</returns>
    public async Task<Response> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("ChangePassword: usuario no encontrado {UserId}", userId);
                return Response.Fail("Usuario no encontrado", USER_NOT_FOUND);
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);

            if (result == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("ChangePassword: password incorrecta {UserId}", userId);
                return Response.Fail("Contraseña actual incorrecta", WRONG_CURRENT_PASSWORD);
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            user.PasswordChangedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Password actualizado {UserId}", userId);

            return Response.Ok(null, "Contraseña actualizada correctamente", PASSWORD_CHANGE_SUCCESS);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ChangePasswordAsync {UserId}", userId);
            return Response.Fail("Error interno del servidor", INTERNAL_ERROR);
        }
    }

    /// <summary>
    /// Inicia el proceso de cambio de email del usuario: valida que el nuevo email no esté en uso
    /// por otra cuenta y envía un código de verificación a esa dirección reutilizando la
    /// infraestructura de generación y envío de códigos de <see cref="IVerificationCodeService"/>.
    /// El email del usuario no se modifica hasta que el código sea confirmado.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="dto">Datos con el nuevo email a verificar.</param>
    /// <returns>Una respuesta indicando si el código fue enviado correctamente o el motivo del error.</returns>
    public async Task<Response> RequestEmailChangeAsync(int userId, RequestEmailChangeDto dto)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("RequestEmailChange: usuario no encontrado {UserId}", userId);
                return Response.Fail("Usuario no encontrado", USER_NOT_FOUND);
            }

            if (string.Equals(user.Email, dto.NewEmail, StringComparison.OrdinalIgnoreCase))
                return Response.Fail("El nuevo email debe ser distinto al actual", EMAIL_SAME_AS_CURRENT);

            var existingUser = await _userRepository.GetByEmailAsync(dto.NewEmail);

            if (existingUser != null)
                return Response.Fail("Ya existe una cuenta registrada con ese email", EMAIL_ALREADY_EXISTS);

            var emailSent = await _verificationCodeService.GenerateAndSendCodeAsync(user, "Verificación de cambio de email", dto.NewEmail);

            if (!emailSent)
            {
                _logger.LogWarning("RequestEmailChange: no se pudo enviar el código {UserId}", userId);
                return Response.Fail("No pudimos enviar el código de verificación. Intentá nuevamente.", EMAIL_VERIFICATION_SEND_FAILED);
            }

            _logger.LogInformation("Código de cambio de email enviado {UserId}", userId);

            return Response.Ok(new { newEmail = dto.NewEmail }, "Te enviamos un código de verificación a tu nuevo email", EMAIL_VERIFICATION_SENT);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RequestEmailChangeAsync {UserId}", userId);
            return Response.Fail("Error interno del servidor", INTERNAL_ERROR);
        }
    }

    /// <summary>
    /// Confirma el cambio de email del usuario validando el código de verificación enviado al nuevo
    /// email. Si el código es correcto, actualiza el email del usuario; en caso contrario, no
    /// realiza ningún cambio y permite reintentar.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="dto">Datos con el código de verificación ingresado.</param>
    /// <returns>Una respuesta indicando si el email fue actualizado o el motivo del error.</returns>
    public async Task<Response> ConfirmEmailChangeAsync(int userId, ConfirmEmailChangeDto dto)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("ConfirmEmailChange: usuario no encontrado {UserId}", userId);
                return Response.Fail("Usuario no encontrado", USER_NOT_FOUND);
            }

            var validation = await _verificationCodeService.ValidateCodeAsync(user, dto.Code);

            if (!validation.Success)
            {
                _logger.LogWarning("ConfirmEmailChange: {Reason} {UserId}", validation.ErrorMessage, userId);
                return Response.Fail(validation.ErrorMessage!, validation.ErrorCode);
            }

            var credential = validation.Credential!;

            if (string.IsNullOrEmpty(credential.PendingEmail))
            {
                _logger.LogWarning("ConfirmEmailChange: no hay un email pendiente de confirmación {UserId}", userId);
                return Response.Fail("No hay un cambio de email pendiente de confirmación", NO_PENDING_EMAIL_CHANGE);
            }

            var newEmail = credential.PendingEmail;

            credential.IsUsed = true;
            credential.PendingEmail = null;

            user.Email = newEmail;
            user.UpdateAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Email actualizado correctamente {UserId}", userId);

            return Response.Ok(new { email = newEmail }, "Email actualizado correctamente", EMAIL_CHANGE_SUCCESS);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ConfirmEmailChangeAsync {UserId}", userId);
            return Response.Fail("Error interno del servidor", INTERNAL_ERROR);
        }
    }

    /// <summary>
    /// Sube y reemplaza la imagen de perfil del usuario, eliminando la anterior si existía.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="file">Archivo de imagen a subir.</param>
    /// <returns>Una respuesta con la nueva URL de la imagen de perfil o un mensaje de error.</returns>
    public async Task<Response> UploadProfileImageAsync(int userId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return Response.Fail("Archivo inválido", INVALID_FILE);

            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("UploadImage: usuario no encontrado {UserId}", userId);
                return Response.Fail("Usuario no encontrado", USER_NOT_FOUND);
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

            return Response.Ok(new { profileImageUrl = user.ProfileImageUrl }, "Imagen actualizada", IMAGE_UPLOAD_SUCCESS);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UploadProfileImageAsync {UserId}", userId);
            return Response.Fail("Error interno del servidor", INTERNAL_ERROR);
        }
    }

    /// <summary>
    /// Elimina la cuenta del usuario y todos los datos relacionados (imagen de perfil, historial de carteras,
    /// notificaciones, alertas, favoritos, transacciones, carteras, tokens y credenciales temporales).
    /// </summary>
    /// <param name="userId">Identificador del usuario a eliminar.</param>
    /// <returns>Una respuesta indicando si la eliminación de la cuenta fue exitosa o el motivo del error.</returns>
    public async Task<Response> DeleteAccountAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("DeleteAccount: usuario no encontrado {UserId}", userId);
                return Response.Fail("Usuario no encontrado", USER_NOT_FOUND);
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

            return Response.Ok(null, "Cuenta eliminada correctamente", ACCOUNT_DELETED);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en DeleteAccountAsync {UserId}", userId);
            return Response.Fail("Error interno del servidor", INTERNAL_ERROR);
        }
    }
}