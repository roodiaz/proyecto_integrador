using InvestLab.Business.Interfaces.Api;
using InvestLab.Business.Security;
using InvestLab.Models.DTOs.Auth;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models;
using InvestLab.Models.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace InvestLab.Tests.Business.Services.Api;

/// <summary>
/// Pruebas unitarias de <see cref="AuthService"/>, cubriendo los métodos invocados desde <c>AuthController</c>:
/// registro, verificación de cuenta, reenvío de código, login, renovación de tokens y logout.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserTempCredentialRepository> _tempRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IUserSettingRepository> _userSettingRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IPasswordHasher<User>> _passwordHasher = new();
    private readonly Mock<ILogger<AuthService>> _logger = new();
    private readonly Mock<IEmailProvider> _emailProvider = new();
    private readonly Mock<IEmailProviderResolver> _emailProviderResolver = new();
    private readonly Mock<IVerificationCodeService> _verificationCodeService = new();
    private readonly LimitsOptions _limits = new() { InitialBalance = 10000 };
    private readonly JwtSettings _jwtSettings = new() { SecretKey = "test", Issuer = "test", Audience = "test", ExpirationMinutes = 15, RefreshTokenExpirationDays = 7 };

    private AuthService CreateService()
    {
        _emailProviderResolver.Setup(x => x.GetProvider()).Returns(_emailProvider.Object);
        return new(_userRepository.Object, _tempRepository.Object, _refreshTokenRepository.Object, _userSettingRepository.Object, _unitOfWork.Object, _jwtService.Object, _passwordHasher.Object, _logger.Object, _emailProviderResolver.Object, _verificationCodeService.Object, Options.Create(_limits), Options.Create(_jwtSettings));
    }

    private static User UserEntity(int id = 1, string email = "user@test.com", bool isActive = true, string passwordHash = "hash") =>
        new() { Id = id, Username = "user", Email = email, PasswordHash = passwordHash, Phone = "123", IsActive = isActive, CreatedAt = DateTime.UtcNow };
    private static RegisterDto RegisterDtoOf(string email = "new@test.com", string password = "secret1", string confirm = "secret1") =>
        new() { FullName = "Nombre Apellido", Email = email, Phone = "123", Password = password, ConfirmPassword = confirm };
    private static UserTempCredential TempCredential(int userId = 1, string hash = "codehash", bool used = false, DateTime? expiresAt = null) =>
        new() { Id = 1, UserId = userId, TempPasswordHash = hash, IsUsed = used, ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(15) };
    private static RefreshToken RefreshTokenEntity(int userId = 1, string token = "tok", bool revoked = false, DateTime? expiresAt = null) =>
        new() { Id = 1, UserId = userId, Token = token, IsRevoked = revoked, ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(1), User = UserEntity(userId) };

    private void SetupHasher(PasswordVerificationResult result) =>
        _passwordHasher.Setup(h => h.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>())).Returns(result);

    // ---------- RegisterAsync ----------

    /// <summary>Verifica que, con datos válidos y sin un usuario previo, se cree el usuario, su configuración, su credencial temporal y se envíe el correo de verificación.</summary>
    [Fact]
    public async Task RegisterAsync_WhenDataIsValid_ShouldCreateUserAndReturnSuccessResponse()
    {
        User? createdUser = null;
        _userRepository.Setup(r => r.GetByEmailAsync("new@test.com")).ReturnsAsync((User?)null);
        _userRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Callback<User>(u => createdUser = u);
        _passwordHasher.Setup(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>())).Returns("hashed");
        _verificationCodeService.Setup(v => v.GenerateAndSendCodeAsync(It.IsAny<User>(), "Verificación de cuenta", null)).ReturnsAsync(true);

        var result = await CreateService().RegisterAsync(RegisterDtoOf());

        Assert.True(result.Success);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _verificationCodeService.Verify(v => v.GenerateAndSendCodeAsync(It.IsAny<User>(), "Verificación de cuenta", null), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
        Assert.NotNull(createdUser);
    }

    /// <summary>Verifica que, si la contraseña y su confirmación no coinciden, se devuelva una respuesta de error sin crear el usuario.</summary>
    [Fact]
    public async Task RegisterAsync_WhenPasswordsDoNotMatch_ShouldReturnErrorResponse()
    {
        var result = await CreateService().RegisterAsync(RegisterDtoOf(password: "secret1", confirm: "other2"));

        Assert.False(result.Success);
        Assert.Equal("Las contraseñas no coinciden", result.Message);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    /// <summary>Verifica que, si el email ya pertenece a una cuenta activa, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyRegisteredAndActive_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("new@test.com")).ReturnsAsync(UserEntity(isActive: true));

        var result = await CreateService().RegisterAsync(RegisterDtoOf());

        Assert.False(result.Success);
        Assert.Equal("El email ya está registrado", result.Message);
    }

    /// <summary>Verifica que, si el email pertenece a una cuenta inactiva, se reenvíe el código de verificación en lugar de crear un usuario nuevo.</summary>
    [Fact]
    public async Task RegisterAsync_WhenEmailExistsButIsInactive_ShouldResendVerificationAndReturnSuccessResponse()
    {
        var existing = UserEntity(isActive: false);
        _userRepository.Setup(r => r.GetByEmailAsync("new@test.com")).ReturnsAsync(existing);
        _verificationCodeService.Setup(v => v.GenerateAndSendCodeAsync(existing, "Verificación de cuenta", null)).ReturnsAsync(true);

        var result = await CreateService().RegisterAsync(RegisterDtoOf());

        Assert.True(result.Success);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        _verificationCodeService.Verify(v => v.GenerateAndSendCodeAsync(existing, "Verificación de cuenta", null), Times.Once);
    }

    /// <summary>Verifica el caso borde donde el envío del correo de verificación falla: la cuenta debe crearse igual y la respuesta seguir siendo exitosa con <c>emailSent = false</c>.</summary>
    [Fact]
    public async Task RegisterAsync_WhenEmailSendingFails_ShouldStillReturnSuccessResponseWithEmailSentFalse()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("new@test.com")).ReturnsAsync((User?)null);
        _passwordHasher.Setup(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>())).Returns("hashed");
        _verificationCodeService.Setup(v => v.GenerateAndSendCodeAsync(It.IsAny<User>(), "Verificación de cuenta", null)).ReturnsAsync(false);

        var result = await CreateService().RegisterAsync(RegisterDtoOf());

        Assert.True(result.Success);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    /// <summary>Verifica que, ante una excepción del repositorio, se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task RegisterAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().RegisterAsync(RegisterDtoOf());

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    // ---------- VerifyAsync ----------

    /// <summary>Verifica que, con un código válido y vigente, se active la cuenta, se marque el código como usado y se devuelva éxito.</summary>
    [Fact]
    public async Task VerifyAsync_WhenCodeIsValid_ShouldActivateUserAndReturnSuccessResponse()
    {
        var user = UserEntity(isActive: false);
        var temp = TempCredential(user.Id);
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _verificationCodeService.Setup(v => v.ValidateCodeAsync(user, "123456")).ReturnsAsync(VerificationCodeResult.Ok(temp));

        var result = await CreateService().VerifyAsync(new VerifyDto { Email = user.Email, Code = "123456" });

        Assert.True(result.Success);
        Assert.True(user.IsActive);
        Assert.True(temp.IsUsed);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, si no existe un usuario con el email indicado, se devuelva el mismo error que un código inválido (sin revelar que la cuenta no existe).</summary>
    [Fact]
    public async Task VerifyAsync_WhenUserDoesNotExist_ShouldReturnInvalidCodeErrorResponse()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await CreateService().VerifyAsync(new VerifyDto { Email = "x@test.com", Code = "123456" });

        Assert.False(result.Success);
        Assert.Equal("Código inválido", result.Message);
    }

    /// <summary>Verifica que, si la cuenta ya está activa, se devuelva una respuesta exitosa indicando que ya fue verificada.</summary>
    [Fact]
    public async Task VerifyAsync_WhenUserIsAlreadyActive_ShouldReturnSuccessResponseWithMessage()
    {
        var user = UserEntity(isActive: true);
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var result = await CreateService().VerifyAsync(new VerifyDto { Email = user.Email, Code = "123456" });

        Assert.True(result.Success);
        Assert.Equal("La cuenta ya se encuentra verificada", result.Message);
    }

    /// <summary>Verifica que, si no existe una credencial temporal asociada al usuario, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task VerifyAsync_WhenCodeDoesNotExist_ShouldReturnErrorResponse()
    {
        var user = UserEntity(isActive: false);
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _verificationCodeService.Setup(v => v.ValidateCodeAsync(user, "123456")).ReturnsAsync(VerificationCodeResult.Fail("Código no encontrado"));

        var result = await CreateService().VerifyAsync(new VerifyDto { Email = user.Email, Code = "123456" });

        Assert.False(result.Success);
        Assert.Equal("Código no encontrado", result.Message);
    }

    /// <summary>Verifica que, si el código de verificación está expirado, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task VerifyAsync_WhenCodeIsExpired_ShouldReturnErrorResponse()
    {
        var user = UserEntity(isActive: false);
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _verificationCodeService.Setup(v => v.ValidateCodeAsync(user, "123456")).ReturnsAsync(VerificationCodeResult.Fail("Código expirado"));

        var result = await CreateService().VerifyAsync(new VerifyDto { Email = user.Email, Code = "123456" });

        Assert.False(result.Success);
        Assert.Equal("Código expirado", result.Message);
    }

    /// <summary>Verifica que, si el código ya fue utilizado previamente, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task VerifyAsync_WhenCodeWasAlreadyUsed_ShouldReturnErrorResponse()
    {
        var user = UserEntity(isActive: false);
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _verificationCodeService.Setup(v => v.ValidateCodeAsync(user, "123456")).ReturnsAsync(VerificationCodeResult.Fail("El código ya fue utilizado"));

        var result = await CreateService().VerifyAsync(new VerifyDto { Email = user.Email, Code = "123456" });

        Assert.False(result.Success);
        Assert.Equal("El código ya fue utilizado", result.Message);
    }

    /// <summary>Verifica que, si el código ingresado no coincide con el hash almacenado, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task VerifyAsync_WhenCodeIsInvalid_ShouldReturnErrorResponse()
    {
        var user = UserEntity(isActive: false);
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _verificationCodeService.Setup(v => v.ValidateCodeAsync(user, "wrong")).ReturnsAsync(VerificationCodeResult.Fail("Código inválido"));

        var result = await CreateService().VerifyAsync(new VerifyDto { Email = user.Email, Code = "wrong" });

        Assert.False(result.Success);
        Assert.Equal("Código inválido", result.Message);
    }

    /// <summary>Verifica que, ante una excepción del repositorio, se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task VerifyAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().VerifyAsync(new VerifyDto { Email = "x@test.com", Code = "123456" });

        Assert.False(result.Success);
        Assert.Equal("Error interno del servidor", result.Message);
    }

    // ---------- ResendCodeAsync ----------

    /// <summary>Verifica que, si el usuario existe y está inactivo, se reenvíe el código de verificación y se devuelva una respuesta exitosa.</summary>
    [Fact]
    public async Task ResendCodeAsync_WhenUserExistsAndIsInactive_ShouldReturnSuccessResponse()
    {
        var user = UserEntity(isActive: false);
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _verificationCodeService.Setup(v => v.GenerateAndSendCodeAsync(user, "Verificación de cuenta", null)).ReturnsAsync(true);

        var result = await CreateService().ResendCodeAsync(new ResendCodeDto { Email = user.Email });

        Assert.True(result.Success);
        _verificationCodeService.Verify(v => v.GenerateAndSendCodeAsync(user, "Verificación de cuenta", null), Times.Once);
    }

    /// <summary>Verifica que, si no existe un usuario con el email indicado, se devuelva una respuesta genérica de éxito (sin revelar que la cuenta no existe).</summary>
    [Fact]
    public async Task ResendCodeAsync_WhenUserDoesNotExist_ShouldReturnGenericSuccessResponse()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await CreateService().ResendCodeAsync(new ResendCodeDto { Email = "x@test.com" });

        Assert.True(result.Success);
        Assert.Equal("Si la cuenta existe y no está verificada, te enviamos un nuevo código", result.Message);
        _verificationCodeService.Verify(v => v.GenerateAndSendCodeAsync(It.IsAny<User>(), It.IsAny<string>(), null), Times.Never);
    }

    /// <summary>Verifica que, si la cuenta ya está verificada, se devuelva una respuesta de error indicando que no corresponde reenviar el código.</summary>
    [Fact]
    public async Task ResendCodeAsync_WhenUserIsAlreadyActive_ShouldReturnErrorResponse()
    {
        var user = UserEntity(isActive: true);
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var result = await CreateService().ResendCodeAsync(new ResendCodeDto { Email = user.Email });

        Assert.False(result.Success);
        Assert.Equal("La cuenta ya está verificada", result.Message);
    }

    /// <summary>Verifica que, ante una excepción del repositorio, se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task ResendCodeAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().ResendCodeAsync(new ResendCodeDto { Email = "x@test.com" });

        Assert.False(result.Success);
        Assert.Equal("Error interno del servidor", result.Message);
    }

    // ---------- LoginAsync ----------

    /// <summary>Verifica que, con credenciales válidas y cuenta verificada, se generen y persistan los tokens y se devuelva una respuesta exitosa con los datos de acceso.</summary>
    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ShouldReturnSuccessResponseWithTokens()
    {
        var user = UserEntity();
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        SetupHasher(PasswordVerificationResult.Success);
        _jwtService.Setup(j => j.GenerateTokensAsync(user)).ReturnsAsync(new TokenDto { AccessToken = "access", RefreshToken = "refresh", ExpiresAt = DateTime.UtcNow.AddHours(1) });

        var result = await CreateService().LoginAsync(new LoginDto { Email = user.Email, Password = "secret1" });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, si no existe un usuario con el email indicado, se devuelva un error genérico de credenciales incorrectas.</summary>
    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await CreateService().LoginAsync(new LoginDto { Email = "x@test.com", Password = "secret1" });

        Assert.False(result.Success);
        Assert.Equal("Email o contraseña incorrectos", result.Message);
    }

    /// <summary>Verifica que, si la contraseña ingresada es incorrecta, se devuelva un error genérico de credenciales incorrectas.</summary>
    [Fact]
    public async Task LoginAsync_WhenPasswordIsIncorrect_ShouldReturnErrorResponse()
    {
        var user = UserEntity();
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        SetupHasher(PasswordVerificationResult.Failed);

        var result = await CreateService().LoginAsync(new LoginDto { Email = user.Email, Password = "wrong" });

        Assert.False(result.Success);
        Assert.Equal("Email o contraseña incorrectos", result.Message);
    }

    /// <summary>Verifica que, si la cuenta no fue verificada, se devuelva una respuesta de error indicando que debe verificarse primero.</summary>
    [Fact]
    public async Task LoginAsync_WhenAccountIsNotVerified_ShouldReturnErrorResponse()
    {
        var user = UserEntity(isActive: false);
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        SetupHasher(PasswordVerificationResult.Success);

        var result = await CreateService().LoginAsync(new LoginDto { Email = user.Email, Password = "secret1" });

        Assert.False(result.Success);
        Assert.Equal("Debes verificar tu cuenta", result.Message);
    }

    /// <summary>Verifica que, ante una excepción del repositorio, se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task LoginAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().LoginAsync(new LoginDto { Email = "x@test.com", Password = "secret1" });

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    // ---------- RefreshTokenAsync ----------

    /// <summary>Verifica que, con un refresh token válido y vigente, se revoque el token anterior, se genere uno nuevo y se devuelva una respuesta exitosa.</summary>
    [Fact]
    public async Task RefreshTokenAsync_WhenTokenIsValid_ShouldReturnSuccessResponseWithNewTokens()
    {
        var stored = RefreshTokenEntity();
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync(TokenHasher.Hash(stored.Token))).ReturnsAsync(stored);
        _jwtService.Setup(j => j.GenerateTokensAsync(stored.User)).ReturnsAsync(new TokenDto { AccessToken = "access2", RefreshToken = "refresh2", ExpiresAt = DateTime.UtcNow.AddHours(1) });

        var result = await CreateService().RefreshTokenAsync(stored.Token);

        Assert.True(result.Success);
        Assert.True(stored.IsRevoked);
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, si el token no existe o ya fue revocado, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task RefreshTokenAsync_WhenTokenDoesNotExistOrIsRevoked_ShouldReturnErrorResponse()
    {
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>())).ReturnsAsync((RefreshToken?)null);

        var result = await CreateService().RefreshTokenAsync("missing");

        Assert.False(result.Success);
        Assert.Equal("Token inválido", result.Message);
    }

    /// <summary>Verifica que, si el refresh token está expirado, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task RefreshTokenAsync_WhenTokenIsExpired_ShouldReturnErrorResponse()
    {
        var stored = RefreshTokenEntity(expiresAt: DateTime.UtcNow.AddDays(-1));
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync(TokenHasher.Hash(stored.Token))).ReturnsAsync(stored);

        var result = await CreateService().RefreshTokenAsync(stored.Token);

        Assert.False(result.Success);
        Assert.Equal("Token expirado", result.Message);
    }

    /// <summary>Verifica que, ante una excepción del repositorio, se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task RefreshTokenAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().RefreshTokenAsync("tok");

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    // ---------- LogoutAsync ----------

    /// <summary>Verifica que, con un refresh token válido, se revoque el token y se devuelva una respuesta exitosa.</summary>
    [Fact]
    public async Task LogoutAsync_WhenTokenIsValid_ShouldRevokeTokenAndReturnSuccessResponse()
    {
        var stored = RefreshTokenEntity();
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync(TokenHasher.Hash(stored.Token))).ReturnsAsync(stored);

        var result = await CreateService().LogoutAsync(stored.Token);

        Assert.True(result.Success);
        Assert.True(stored.IsRevoked);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica el caso borde donde el token no existe o ya fue revocado: el servicio responde éxito igualmente y no persiste cambios.</summary>
    [Fact]
    public async Task LogoutAsync_WhenTokenDoesNotExistOrIsAlreadyRevoked_ShouldReturnSuccessResponseWithoutChanges()
    {
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>())).ReturnsAsync((RefreshToken?)null);

        var result = await CreateService().LogoutAsync("missing");

        Assert.True(result.Success);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    /// <summary>Verifica que, ante una excepción del repositorio, se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task LogoutAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().LogoutAsync("tok");

        Assert.False(result.Success);
        Assert.Equal("Error interno del servidor", result.Message);
    }

    // ---------- ForgotPasswordAsync ----------

    /// <summary>Verifica que, si no existe una cuenta asociada al email indicado, se devuelva una respuesta genérica de éxito (sin revelar que la cuenta no existe).</summary>
    [Fact]
    public async Task ForgotPasswordAsync_WhenUserDoesNotExist_ShouldReturnGenericSuccessResponse()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await CreateService().ForgotPasswordAsync(new ForgotPasswordDto { Email = "x@test.com" });

        Assert.True(result.Success);
        Assert.Equal("Si existe una cuenta asociada a ese email, te enviamos un código de recuperación", result.Message);
        _verificationCodeService.Verify(v => v.GenerateAndSendCodeAsync(It.IsAny<User>(), It.IsAny<string>(), null), Times.Never);
    }

    /// <summary>Verifica que, si el usuario existe, se genere y envíe el código de recuperación y se devuelva la respuesta genérica de éxito.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_WhenEmailIsSentSuccessfully_ShouldReturnGenericSuccessResponse()
    {
        var user = UserEntity();
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _verificationCodeService.Setup(v => v.GenerateAndSendCodeAsync(user, "Recuperación de contraseña", null)).ReturnsAsync(true);

        var result = await CreateService().ForgotPasswordAsync(new ForgotPasswordDto { Email = user.Email });

        Assert.True(result.Success);
        Assert.Equal("Si existe una cuenta asociada a ese email, te enviamos un código de recuperación", result.Message);
        _verificationCodeService.Verify(v => v.GenerateAndSendCodeAsync(user, "Recuperación de contraseña", null), Times.Once);
    }

    /// <summary>Verifica que, si el envío del correo falla, se devuelva igualmente la respuesta genérica de éxito (sin revelar el fallo de envío).</summary>
    [Fact]
    public async Task ForgotPasswordAsync_WhenEmailSendingFails_ShouldReturnGenericSuccessResponse()
    {
        var user = UserEntity();
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _verificationCodeService.Setup(v => v.GenerateAndSendCodeAsync(user, "Recuperación de contraseña", null)).ReturnsAsync(false);

        var result = await CreateService().ForgotPasswordAsync(new ForgotPasswordDto { Email = user.Email });

        Assert.True(result.Success);
        Assert.Equal("Si existe una cuenta asociada a ese email, te enviamos un código de recuperación", result.Message);
    }

    /// <summary>Verifica que, ante una excepción del repositorio, se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().ForgotPasswordAsync(new ForgotPasswordDto { Email = "x@test.com" });

        Assert.False(result.Success);
        Assert.Equal("Error interno del servidor", result.Message);
    }

    // ---------- ResetPasswordAsync ----------

    /// <summary>Verifica que, si la nueva contraseña y su confirmación no coinciden, se devuelva una respuesta de error sin consultar al repositorio.</summary>
    [Fact]
    public async Task ResetPasswordAsync_WhenPasswordsDoNotMatch_ShouldReturnErrorResponse()
    {
        var dto = new ResetPasswordDto { Email = "user@test.com", Code = "123456", NewPassword = "secret1", ConfirmPassword = "secret2" };

        var result = await CreateService().ResetPasswordAsync(dto);

        Assert.False(result.Success);
        Assert.Equal("Las contraseñas no coinciden", result.Message);
        _userRepository.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifica que, si no existe un usuario con el email indicado, se devuelva el mismo error que un código inválido (sin revelar que la cuenta no existe).</summary>
    [Fact]
    public async Task ResetPasswordAsync_WhenUserDoesNotExist_ShouldReturnInvalidCodeErrorResponse()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await CreateService().ResetPasswordAsync(new ResetPasswordDto { Email = "x@test.com", Code = "123456", NewPassword = "secret1", ConfirmPassword = "secret1" });

        Assert.False(result.Success);
        Assert.Equal("Código inválido", result.Message);
    }

    /// <summary>Verifica que, si el código de verificación es inválido o expirado, se devuelva la respuesta de error informada por el servicio de validación.</summary>
    [Fact]
    public async Task ResetPasswordAsync_WhenCodeIsInvalid_ShouldReturnErrorResponse()
    {
        var user = UserEntity();
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _verificationCodeService.Setup(v => v.ValidateCodeAsync(user, "wrong")).ReturnsAsync(VerificationCodeResult.Fail("Código inválido"));

        var result = await CreateService().ResetPasswordAsync(new ResetPasswordDto { Email = user.Email, Code = "wrong", NewPassword = "secret1", ConfirmPassword = "secret1" });

        Assert.False(result.Success);
        Assert.Equal("Código inválido", result.Message);
        _passwordHasher.Verify(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifica que, con un código válido, se actualice la contraseña, se marque la credencial como usada, se revoquen los refresh tokens y se devuelva éxito.</summary>
    [Fact]
    public async Task ResetPasswordAsync_WhenCodeIsValid_ShouldUpdatePasswordRevokeTokensAndReturnSuccessResponse()
    {
        var user = UserEntity();
        var temp = TempCredential(user.Id);
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _verificationCodeService.Setup(v => v.ValidateCodeAsync(user, "123456")).ReturnsAsync(VerificationCodeResult.Ok(temp));
        _passwordHasher.Setup(h => h.HashPassword(user, "newSecret1")).Returns("newHashedPassword");

        var result = await CreateService().ResetPasswordAsync(new ResetPasswordDto { Email = user.Email, Code = "123456", NewPassword = "newSecret1", ConfirmPassword = "newSecret1" });

        Assert.True(result.Success);
        Assert.Equal("Contraseña actualizada correctamente", result.Message);
        Assert.True(temp.IsUsed);
        Assert.Equal("newHashedPassword", user.PasswordHash);
        Assert.NotNull(user.PasswordChangedAt);
        _refreshTokenRepository.Verify(r => r.RevokeAllByUserIdAsync(user.Id), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, ante una excepción del repositorio, se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task ResetPasswordAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().ResetPasswordAsync(new ResetPasswordDto { Email = "x@test.com", Code = "123456", NewPassword = "secret1", ConfirmPassword = "secret1" });

        Assert.False(result.Success);
        Assert.Equal("Error interno del servidor", result.Message);
    }
}
