using InvestLab.Business.Interfaces.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static InvestLab.Models.MessageCodes;

namespace InvestLab.Business.Services.Api
{
    /// <summary>
    /// Implementación del servicio de generación, envío y validación de códigos de verificación
    /// temporales, basada en <see cref="UserTempCredential"/>. Centraliza la lógica utilizada
    /// originalmente por el registro de cuentas para que pueda reutilizarse en otros flujos
    /// (cambio de email, futuras verificaciones) sin duplicar código.
    /// </summary>
    public class VerificationCodeService : IVerificationCodeService
    {
        private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);

        private readonly IUserTempCredentialRepository _tempRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IEmailProviderResolver _emailProviderResolver;
        private readonly ILogger<VerificationCodeService> _logger;

        public VerificationCodeService(IUserTempCredentialRepository tempRepository, IUnitOfWork unitOfWork, IPasswordHasher<User> passwordHasher, IEmailProviderResolver emailProviderResolver, ILogger<VerificationCodeService> logger)
        {
            _tempRepository = tempRepository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _emailProviderResolver = emailProviderResolver;
            _logger = logger;
        }

        public async Task<bool> GenerateAndSendCodeAsync(User user, string emailSubject, string? pendingEmail = null)
        {
            var code = new Random().Next(100000, 999999).ToString();
            var codeHash = _passwordHasher.HashPassword(user, code);

            var existing = await _tempRepository.GetByUserIdAsync(user.Id);

            if (existing is null)
            {
                var tempCredential = new UserTempCredential
                {
                    UserId = user.Id,
                    TempPasswordHash = codeHash,
                    PendingEmail = pendingEmail,
                    ExpiresAt = DateTime.UtcNow.Add(CodeLifetime),
                    IsUsed = false
                };

                await _tempRepository.AddAsync(tempCredential);
            }
            else
            {
                existing.TempPasswordHash = codeHash;
                existing.PendingEmail = pendingEmail;
                existing.ExpiresAt = DateTime.UtcNow.Add(CodeLifetime);
                existing.IsUsed = false;
            }

            await _unitOfWork.SaveChangesAsync();

            var destinationEmail = pendingEmail ?? user.Email;

            try
            {
                await _emailProviderResolver.GetProvider().SendAsync(
                    destinationEmail,
                    emailSubject,
                    EmailTemplates.VerificationCode(code)
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el código de verificación a {Email}", destinationEmail);
                return false;
            }
        }

        public async Task<VerificationCodeResult> ValidateCodeAsync(User user, string code)
        {
            var temp = await _tempRepository.GetByUserIdAsync(user.Id);

            if (temp == null)
                return VerificationCodeResult.Fail("Código no encontrado", VERIFICATION_CODE_NOT_FOUND);

            if (temp.ExpiresAt < DateTime.UtcNow)
                return VerificationCodeResult.Fail("Código expirado", VERIFICATION_CODE_EXPIRED);

            if (temp.IsUsed)
                return VerificationCodeResult.Fail("El código ya fue utilizado", VERIFICATION_CODE_ALREADY_USED);

            var result = _passwordHasher.VerifyHashedPassword(user, temp.TempPasswordHash, code);

            if (result == PasswordVerificationResult.Failed)
                return VerificationCodeResult.Fail("Código inválido", VERIFICATION_CODE_INVALID);

            return VerificationCodeResult.Ok(temp);
        }
    }
}
