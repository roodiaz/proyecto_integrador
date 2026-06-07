using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Models.DTOs.Auth;
using InvestLab.Models.DTOs.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InvestLab.Tests.Business.Services.Api;

/// <summary>
/// Pruebas unitarias de <see cref="UserService"/>, cubriendo los métodos invocados desde <c>UserController</c>
/// para consultar y actualizar el perfil, cambiar la contraseña, gestionar la imagen de perfil y eliminar la cuenta del usuario autenticado.
/// </summary>
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher<User>> _passwordHasher = new();
    private readonly Mock<ILogger<UserService>> _logger = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserSettingRepository> _userSettingRepository = new();
    private readonly Mock<IPortfolioRepository> _portfolioRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IPortfolioHistoryRepository> _portfolioHistoryRepository = new();
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<IAlertRepository> _alertRepository = new();
    private readonly Mock<IFavoriteRepository> _favoriteRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IUserTempCredentialRepository> _userTempCredentialRepository = new();

    private UserService CreateService() => new(_userRepository.Object, _passwordHasher.Object, _logger.Object, _unitOfWork.Object, _userSettingRepository.Object,
        _portfolioRepository.Object, _transactionRepository.Object, _portfolioHistoryRepository.Object, _notificationRepository.Object, _alertRepository.Object,
        _favoriteRepository.Object, _refreshTokenRepository.Object, _userTempCredentialRepository.Object);

    private static UserSetting Settings(int userId = 1) => new() { Id = 1, UserId = userId, Currency = "USD", EmailNotifications = true };

    private static User UserEntity(int id = 1, string? profileImageUrl = null) => new()
    {
        Id = id,
        Username = "user",
        Email = "user@test.com",
        PasswordHash = "hashed-password",
        Phone = "123456789",
        ProfileImageUrl = profileImageUrl,
        UserSetting = Settings(id)
    };

    private static UpdateProfileDto UpdateProfileData(string userName = "newUserName") => new() { UserName = userName, Phone = "987654321", Currency = "ARS", EmailNotifications = false };

    private static ChangePasswordDto ChangePasswordData(string currentPassword = "oldPassword123", string newPassword = "newPassword456") => new() { CurrentPassword = currentPassword, NewPassword = newPassword };

    private static Mock<IFormFile> FormFile(string fileName = "avatar.png", long length = 10)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.Length).Returns(length);
        file.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return file;
    }

    // ---------- GetProfileAsync ----------

    /// <summary>Verifica que, si el usuario no existe, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetProfileAsync_WhenUserDoesNotExist_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdWithSettingsAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await CreateService().GetProfileAsync(1);

        Assert.False(result.Success);
        Assert.Equal("Usuario no encontrado", result.Message);
    }

    /// <summary>Verifica que, cuando el usuario existe, se devuelva una respuesta exitosa con los datos del perfil y su configuración.</summary>
    [Fact]
    public async Task GetProfileAsync_WhenUserExists_ShouldReturnSuccessResponseWithProfileData()
    {
        _userRepository.Setup(r => r.GetByIdWithSettingsAsync(1)).ReturnsAsync(UserEntity());

        var result = await CreateService().GetProfileAsync(1);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    // ---------- UpdateProfileAsync ----------

    /// <summary>Verifica que, si el usuario no existe, se devuelva una respuesta de error sin guardar cambios.</summary>
    [Fact]
    public async Task UpdateProfileAsync_WhenUserDoesNotExist_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdWithSettingsAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await CreateService().UpdateProfileAsync(1, UpdateProfileData());

        Assert.False(result.Success);
        Assert.Equal("Usuario no encontrado", result.Message);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    /// <summary>Verifica que, con datos válidos, se actualicen los datos personales y la configuración del usuario, y se confirmen los cambios.</summary>
    [Fact]
    public async Task UpdateProfileAsync_WhenDataIsValid_ShouldUpdateProfileAndSettingsAndSaveChanges()
    {
        var user = UserEntity();
        _userRepository.Setup(r => r.GetByIdWithSettingsAsync(1)).ReturnsAsync(user);

        var dto = UpdateProfileData(userName: "updatedName");
        var result = await CreateService().UpdateProfileAsync(1, dto);

        Assert.True(result.Success);
        Assert.Equal("Perfil actualizado correctamente", result.Message);
        Assert.Equal("updatedName", user.Username);
        Assert.Equal("987654321", user.Phone);
        Assert.Equal("ARS", user.UserSetting!.Currency);
        Assert.False(user.UserSetting!.EmailNotifications);
        _userRepository.Verify(r => r.UpdateAsync(user), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, ante una excepción inesperada, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task UpdateProfileAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdWithSettingsAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().UpdateProfileAsync(1, UpdateProfileData());

        Assert.False(result.Success);
        Assert.Equal("Error interno del servidor", result.Message);
    }

    // ---------- ChangePasswordAsync ----------

    /// <summary>Verifica que, si el usuario no existe, se devuelva una respuesta de error sin modificar la contraseña.</summary>
    [Fact]
    public async Task ChangePasswordAsync_WhenUserDoesNotExist_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await CreateService().ChangePasswordAsync(1, ChangePasswordData());

        Assert.False(result.Success);
        Assert.Equal("Usuario no encontrado", result.Message);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    /// <summary>Verifica que, si la contraseña actual ingresada es incorrecta, se devuelva una respuesta de error sin modificar la contraseña.</summary>
    [Fact]
    public async Task ChangePasswordAsync_WhenCurrentPasswordIsIncorrect_ShouldReturnErrorResponse()
    {
        var user = UserEntity();
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, It.IsAny<string>())).Returns(PasswordVerificationResult.Failed);

        var result = await CreateService().ChangePasswordAsync(1, ChangePasswordData(currentPassword: "wrongPassword"));

        Assert.False(result.Success);
        Assert.Equal("Contraseña actual incorrecta", result.Message);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    /// <summary>Verifica que, con datos válidos, se actualice el hash de la contraseña, se registre la fecha de cambio y se confirmen los cambios.</summary>
    [Fact]
    public async Task ChangePasswordAsync_WhenDataIsValid_ShouldUpdatePasswordHashAndSaveChanges()
    {
        var user = UserEntity();
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "oldPassword123")).Returns(PasswordVerificationResult.Success);
        _passwordHasher.Setup(h => h.HashPassword(user, "newPassword456")).Returns("new-hashed-password");

        var result = await CreateService().ChangePasswordAsync(1, ChangePasswordData());

        Assert.True(result.Success);
        Assert.Equal("Contraseña actualizada correctamente", result.Message);
        Assert.Equal("new-hashed-password", user.PasswordHash);
        Assert.NotNull(user.PasswordChangedAt);
        _userRepository.Verify(r => r.UpdateAsync(user), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ---------- UploadProfileImageAsync ----------

    /// <summary>Verifica que, si no se envía un archivo o este está vacío, se devuelva una respuesta de error sin consultar al usuario.</summary>
    [Fact]
    public async Task UploadProfileImageAsync_WhenFileIsNullOrEmpty_ShouldReturnErrorResponse()
    {
        var emptyFile = FormFile(length: 0);

        var result = await CreateService().UploadProfileImageAsync(1, emptyFile.Object);

        Assert.False(result.Success);
        Assert.Equal("Archivo inválido", result.Message);
        _userRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    /// <summary>Verifica que, si el usuario no existe, se devuelva una respuesta de error sin subir la imagen.</summary>
    [Fact]
    public async Task UploadProfileImageAsync_WhenUserDoesNotExist_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await CreateService().UploadProfileImageAsync(1, FormFile().Object);

        Assert.False(result.Success);
        Assert.Equal("Usuario no encontrado", result.Message);
    }

    /// <summary>Verifica que, con datos válidos, se guarde el archivo, se actualice la URL de la imagen de perfil del usuario y se confirmen los cambios.</summary>
    [Fact]
    public async Task UploadProfileImageAsync_WhenDataIsValid_ShouldStoreFileAndUpdateProfileImageUrl()
    {
        var user = UserEntity();
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var result = await CreateService().UploadProfileImageAsync(1, FormFile(fileName: "new-avatar.png").Object);
        try
        {
            Assert.True(result.Success);
            Assert.Equal("Imagen actualizada", result.Message);
            Assert.NotNull(user.ProfileImageUrl);
            Assert.StartsWith("/images/", user.ProfileImageUrl);
            Assert.EndsWith(".png", user.ProfileImageUrl);
            _userRepository.Verify(r => r.UpdateAsync(user), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
        finally
        {
            var savedPath = Path.Combine("wwwroot", "images", Path.GetFileName(user.ProfileImageUrl!));
            if (File.Exists(savedPath)) File.Delete(savedPath);
        }
    }

    /// <summary>Verifica que, ante una excepción inesperada, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task UploadProfileImageAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().UploadProfileImageAsync(1, FormFile().Object);

        Assert.False(result.Success);
        Assert.Equal("Error interno del servidor", result.Message);
    }

    // ---------- DeleteAccountAsync ----------

    /// <summary>Verifica que, si el usuario no existe, se devuelva una respuesta de error sin eliminar datos asociados.</summary>
    [Fact]
    public async Task DeleteAccountAsync_WhenUserDoesNotExist_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await CreateService().DeleteAccountAsync(1);

        Assert.False(result.Success);
        Assert.Equal("Usuario no encontrado", result.Message);
        _userRepository.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Never);
    }

    /// <summary>Verifica que, con datos válidos, se eliminen todos los datos asociados al usuario, se elimine la cuenta y se confirmen los cambios.</summary>
    [Fact]
    public async Task DeleteAccountAsync_WhenDataIsValid_ShouldRemoveAllRelatedDataAndDeleteAccount()
    {
        var user = UserEntity();
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var result = await CreateService().DeleteAccountAsync(1);

        Assert.True(result.Success);
        Assert.Equal("Cuenta eliminada correctamente", result.Message);
        _portfolioHistoryRepository.Verify(r => r.DeleteByUserIdAsync(1), Times.Once);
        _notificationRepository.Verify(r => r.DeleteByUserIdAsync(1), Times.Once);
        _alertRepository.Verify(r => r.DeleteByUserIdAsync(1), Times.Once);
        _favoriteRepository.Verify(r => r.DeleteByUserIdAsync(1), Times.Once);
        _transactionRepository.Verify(r => r.DeleteByUserIdAsync(1), Times.Once);
        _portfolioRepository.Verify(r => r.DeleteByUserIdAsync(1), Times.Once);
        _refreshTokenRepository.Verify(r => r.DeleteByUserIdAsync(1), Times.Once);
        _userTempCredentialRepository.Verify(r => r.DeleteByUserIdAsync(1), Times.Once);
        _userSettingRepository.Verify(r => r.DeleteByUserIdAsync(1), Times.Once);
        _userRepository.Verify(r => r.DeleteAsync(user), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, ante una excepción inesperada durante la eliminación, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task DeleteAccountAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(UserEntity());
        _portfolioHistoryRepository.Setup(r => r.DeleteByUserIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().DeleteAccountAsync(1);

        Assert.False(result.Success);
        Assert.Equal("Error interno del servidor", result.Message);
    }
}
