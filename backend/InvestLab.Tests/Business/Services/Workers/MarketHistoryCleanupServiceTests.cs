using InvestLab.Business.Services.Workers;
using InvestLab.Data.Interfaces;
using Moq;
using Xunit;

namespace InvestLab.Tests.Business.Services.Workers;

/// <summary>
/// Pruebas unitarias de <see cref="MarketHistoryCleanupService"/>, cubriendo la eliminación
/// de los registros históricos de mercado con una antigüedad mayor a un año.
/// </summary>
public class MarketHistoryCleanupServiceTests
{
    private readonly Mock<IPriceHistoryRepository> _repository = new();

    private MarketHistoryCleanupService CreateService() => new(_repository.Object);

    // ---------- CleanupOldHistoryAsync ----------

    /// <summary>Verifica que se solicite al repositorio eliminar los registros históricos anteriores a un año desde la fecha actual.</summary>
    [Fact]
    public async Task CleanupOldHistoryAsync_WhenInvoked_ShouldDeleteHistoryOlderThanOneYear()
    {
        DateTime? capturedDate = null;
        _repository.Setup(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>())).Callback<DateTime>(d => capturedDate = d).Returns(Task.CompletedTask);

        await CreateService().CleanupOldHistoryAsync();

        Assert.NotNull(capturedDate);
        Assert.True(Math.Abs((capturedDate!.Value - DateTime.UtcNow.AddYears(-1)).TotalMinutes) < 1);
        _repository.Verify(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>()), Times.Once);
    }
}
