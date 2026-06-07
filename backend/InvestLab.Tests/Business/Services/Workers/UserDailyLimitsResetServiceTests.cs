using InvestLab.Business.Services.Workers;
using InvestLab.Data.Interfaces;
using Moq;
using Xunit;

namespace InvestLab.Tests.Business.Services.Workers;

/// <summary>
/// Pruebas unitarias de <see cref="UserDailyLimitsResetService"/>, cubriendo el reseteo
/// de los límites diarios de uso de todos los usuarios.
/// </summary>
public class UserDailyLimitsResetServiceTests
{
    private readonly Mock<IUserSettingRepository> _repository = new();

    private UserDailyLimitsResetService CreateService() => new(_repository.Object);

    // ---------- ResetDailyLimitsAsync ----------

    /// <summary>Verifica que se delegue al repositorio el reseteo de los límites diarios de todos los usuarios.</summary>
    [Fact]
    public async Task ResetDailyLimitsAsync_WhenInvoked_ShouldDelegateToRepository()
    {
        await CreateService().ResetDailyLimitsAsync();

        _repository.Verify(r => r.ResetDailyLimitsAsync(), Times.Once);
    }

    /// <summary>Verifica que, si el repositorio lanza una excepción, esta se propague sin ser capturada por el servicio.</summary>
    [Fact]
    public async Task ResetDailyLimitsAsync_WhenRepositoryThrows_ShouldPropagateException()
    {
        _repository.Setup(r => r.ResetDailyLimitsAsync()).ThrowsAsync(new Exception("db error"));

        await Assert.ThrowsAsync<Exception>(() => CreateService().ResetDailyLimitsAsync());
    }
}
