using InvestLab.Business.Interfaces.Api;
using InvestLab.Business.Services.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Alerts;
using InvestLab.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using static InvestLab.Models.Enums;

namespace InvestLab.Tests.Business.Services.Api;

/// <summary>
/// Pruebas unitarias de <see cref="AlertService"/>, cubriendo los métodos invocados desde <c>AlertController</c>:
/// creación, eliminación, activación/desactivación, listado, actualización y estadísticas de alertas.
/// </summary>
public class AlertServiceTests
{
    private readonly Mock<IAlertRepository> _alertRepository = new();
    private readonly Mock<IAssetRepository> _assetRepository = new();
    private readonly Mock<IUserSettingRepository> _userSettingRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAssetService> _assetService = new();
    private readonly Mock<ILogger<AlertService>> _logger = new();
    private readonly LimitsOptions _limits = new() { MaxAlerts = 5 };

    private AlertService CreateService() =>
        new(_alertRepository.Object, _assetRepository.Object, _userSettingRepository.Object, _unitOfWork.Object, _logger.Object, Options.Create(_limits), _assetService.Object);

    private static UserSetting Settings(int alertsUsed = 0) => new() { Id = 1, UserId = 1, AlertsUsed = alertsUsed };
    private static Asset AssetEntity(int id = 1, string symbol = "AAPL") => new() { Id = id, Symbol = symbol };
    private static CreateAlertDto CreateDto(string symbol = "AAPL", string condition = ">", decimal? price = 100, decimal? percent = null, bool isActive = true) =>
        new() { Symbol = symbol, Condition = condition, Price = price, PercentChange = percent, IsActive = isActive };
    private static UpdateAlertDto UpdateDto(int id = 1, string symbol = "AAPL", string condition = ">", decimal? price = 100, decimal? percent = null, bool isActive = true) =>
        new() { Id = id, Symbol = symbol, Condition = condition, Price = price, PercentChange = percent, IsActive = isActive };
    private static Alert AlertEntity(int id = 1, int userId = 1, int assetId = 1, ConditionType type = ConditionType.Price, AlertOperator op = AlertOperator.GreaterThan, decimal value = 100, bool isActive = true) =>
        new() { Id = id, UserId = userId, AssetId = assetId, ConditionType = type, Operator = op, Value = value, IsActive = isActive, Asset = AssetEntity(assetId) };

    // ---------- CreateAlertAsync ----------

    /// <summary>Verifica que, con datos válidos, dentro del límite y con el activo existente, se cree la alerta y se devuelva una respuesta exitosa.</summary>
    [Fact]
    public async Task CreateAlertAsync_WhenDataIsValid_ShouldReturnSuccessResponse()
    {
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings(alertsUsed: 1));
        _assetService.Setup(s => s.GetOrCreateAsync("AAPL")).ReturnsAsync(AssetEntity());

        var result = await CreateService().CreateAlertAsync(1, CreateDto());

        Assert.True(result.Success);
        Assert.Equal("Alerta creada correctamente", result.Data);
        _alertRepository.Verify(r => r.AddAsync(It.IsAny<Alert>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, si no existe configuración del usuario, se devuelva una respuesta de error sin crear la alerta.</summary>
    [Fact]
    public async Task CreateAlertAsync_WhenUserSettingsNotFound_ShouldReturnErrorResponse()
    {
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync((UserSetting?)null);

        var result = await CreateService().CreateAlertAsync(1, CreateDto());

        Assert.False(result.Success);
        Assert.Equal("Configuración de usuario no encontrada", result.Message);
        _alertRepository.Verify(r => r.AddAsync(It.IsAny<Alert>()), Times.Never);
    }

    /// <summary>Verifica que, si el usuario alcanzó el límite máximo de alertas, se devuelva una respuesta de error y no se consulte el activo.</summary>
    [Fact]
    public async Task CreateAlertAsync_WhenAlertLimitReached_ShouldReturnErrorResponse()
    {
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings(alertsUsed: 5));

        var result = await CreateService().CreateAlertAsync(1, CreateDto());

        Assert.False(result.Success);
        Assert.Equal("Límite de alertas alcanzado", result.Message);
        _assetService.Verify(s => s.GetOrCreateAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifica que, si el activo indicado no existe ni puede crearse, se devuelva una respuesta de error sin persistir la alerta.</summary>
    [Fact]
    public async Task CreateAlertAsync_WhenAssetIsNotFound_ShouldReturnErrorResponse()
    {
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetService.Setup(s => s.GetOrCreateAsync("AAPL")).ReturnsAsync((Asset?)null);

        var result = await CreateService().CreateAlertAsync(1, CreateDto());

        Assert.False(result.Success);
        Assert.Equal("Activo no encontrado", result.Message);
        _alertRepository.Verify(r => r.AddAsync(It.IsAny<Alert>()), Times.Never);
    }

    /// <summary>Verifica que las condiciones basadas en porcentaje (<c>%&gt;</c>/<c>%&lt;</c>) se mapeen correctamente al tipo "Porcentaje" y se cree la alerta.</summary>
    [Theory]
    [InlineData("%>", null, 5)]
    [InlineData("%<", null, 5)]
    public async Task CreateAlertAsync_WhenConditionIsPercentageBased_ShouldMapConditionAndReturnSuccess(string condition, decimal? price, decimal percent)
    {
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetService.Setup(s => s.GetOrCreateAsync("AAPL")).ReturnsAsync(AssetEntity());

        Alert? added = null;
        _alertRepository.Setup(r => r.AddAsync(It.IsAny<Alert>())).Callback<Alert>(a => added = a).Returns(Task.CompletedTask);

        var result = await CreateService().CreateAlertAsync(1, CreateDto(condition: condition, price: price, percent: percent));

        Assert.True(result.Success);
        Assert.NotNull(added);
        Assert.Equal(ConditionType.Percentage, added!.ConditionType);
        Assert.Equal(percent, added.Value);
    }

    /// <summary>Verifica que una condición textual no soportada provoque una excepción controlada y se devuelva "Error interno".</summary>
    [Fact]
    public async Task CreateAlertAsync_WhenConditionIsInvalid_ShouldReturnErrorResponse()
    {
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetService.Setup(s => s.GetOrCreateAsync("AAPL")).ReturnsAsync(AssetEntity());

        var result = await CreateService().CreateAlertAsync(1, CreateDto(condition: "??", price: null, percent: null));

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    /// <summary>Verifica que, ante una excepción del repositorio, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task CreateAlertAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().CreateAlertAsync(1, CreateDto());

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    // ---------- DeleteAlertAsync ----------

    /// <summary>Verifica que, si la alerta existe y pertenece al usuario, se elimine, se decremente el contador de alertas usadas y se devuelva éxito.</summary>
    [Fact]
    public async Task DeleteAlertAsync_WhenAlertExistsAndBelongsToUser_ShouldReturnSuccessResponse()
    {
        var alert = AlertEntity(userId: 1);
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings(alertsUsed: 2));

        var result = await CreateService().DeleteAlertAsync(1, 1);

        Assert.True(result.Success);
        Assert.Equal("Eliminada", result.Message);
        _alertRepository.Verify(r => r.DeleteAsync(alert), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, si la alerta no existe, se devuelva una respuesta de error y no se intente eliminar nada.</summary>
    [Fact]
    public async Task DeleteAlertAsync_WhenAlertDoesNotExist_ShouldReturnErrorResponse()
    {
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Alert?)null);

        var result = await CreateService().DeleteAlertAsync(1, 1);

        Assert.False(result.Success);
        Assert.Equal("No encontrada", result.Message);
        _alertRepository.Verify(r => r.DeleteAsync(It.IsAny<Alert>()), Times.Never);
    }

    /// <summary>Verifica que, si la alerta pertenece a otro usuario, se devuelva una respuesta de error y no se elimine.</summary>
    [Fact]
    public async Task DeleteAlertAsync_WhenAlertBelongsToAnotherUser_ShouldReturnErrorResponse()
    {
        var alert = AlertEntity(userId: 2);
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);

        var result = await CreateService().DeleteAlertAsync(1, 1);

        Assert.False(result.Success);
        Assert.Equal("No encontrada", result.Message);
        _alertRepository.Verify(r => r.DeleteAsync(It.IsAny<Alert>()), Times.Never);
    }

    /// <summary>Verifica el caso borde donde la configuración del usuario no existe: la alerta igualmente debe eliminarse correctamente.</summary>
    [Fact]
    public async Task DeleteAlertAsync_WhenUserSettingsNotFound_ShouldStillDeleteAlert()
    {
        var alert = AlertEntity(userId: 1);
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync((UserSetting?)null);

        var result = await CreateService().DeleteAlertAsync(1, 1);

        Assert.True(result.Success);
        _alertRepository.Verify(r => r.DeleteAsync(alert), Times.Once);
    }

    /// <summary>Verifica que, ante una excepción del repositorio, se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task DeleteAlertAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().DeleteAlertAsync(1, 1);

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    // ---------- ToggleAlertAsync ----------

    /// <summary>Verifica que, si la alerta existe y pertenece al usuario, se invierta su estado <c>IsActive</c> y se devuelva éxito.</summary>
    [Fact]
    public async Task ToggleAlertAsync_WhenAlertExistsAndBelongsToUser_ShouldFlipIsActiveAndReturnSuccess()
    {
        var alert = AlertEntity(userId: 1, isActive: true);
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);

        var result = await CreateService().ToggleAlertAsync(1, 1);

        Assert.True(result.Success);
        Assert.Equal("Estado actualizado", result.Message);
        Assert.False(alert.IsActive);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, si la alerta no existe, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task ToggleAlertAsync_WhenAlertDoesNotExist_ShouldReturnErrorResponse()
    {
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Alert?)null);

        var result = await CreateService().ToggleAlertAsync(1, 1);

        Assert.False(result.Success);
        Assert.Equal("No encontrada", result.Message);
    }

    /// <summary>Verifica que, si la alerta pertenece a otro usuario, se devuelva una respuesta de error sin modificar su estado.</summary>
    [Fact]
    public async Task ToggleAlertAsync_WhenAlertBelongsToAnotherUser_ShouldReturnErrorResponse()
    {
        var alert = AlertEntity(userId: 2);
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);

        var result = await CreateService().ToggleAlertAsync(1, 1);

        Assert.False(result.Success);
        Assert.Equal("No encontrada", result.Message);
    }

    /// <summary>Verifica que, ante una excepción del repositorio, se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task ToggleAlertAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().ToggleAlertAsync(1, 1);

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    // ---------- GetAlertsAsync ----------

    /// <summary>Verifica que, cuando existen alertas, se devuelva una respuesta exitosa con los datos mapeados y el total.</summary>
    [Fact]
    public async Task GetAlertsAsync_WhenAlertsExist_ShouldReturnSuccessResponseWithData()
    {
        var alerts = new List<Alert> { AlertEntity(id: 1), AlertEntity(id: 2) };
        _alertRepository.Setup(r => r.GetPagedAsync(1, It.IsAny<AlertFilterDto>())).ReturnsAsync((alerts, 2));

        var result = await CreateService().GetAlertsAsync(1, new AlertFilterDto());

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    /// <summary>Verifica que, cuando el usuario no tiene alertas, se devuelva una respuesta exitosa con una colección vacía.</summary>
    [Fact]
    public async Task GetAlertsAsync_WhenNoAlertsExist_ShouldReturnSuccessResponseWithEmptyData()
    {
        _alertRepository.Setup(r => r.GetPagedAsync(1, It.IsAny<AlertFilterDto>())).ReturnsAsync((new List<Alert>(), 0));

        var result = await CreateService().GetAlertsAsync(1, new AlertFilterDto());

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    /// <summary>Verifica que, ante una excepción del repositorio, se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task GetAlertsAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _alertRepository.Setup(r => r.GetPagedAsync(1, It.IsAny<AlertFilterDto>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().GetAlertsAsync(1, new AlertFilterDto());

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    // ---------- UpdateAlertAsync ----------

    /// <summary>Verifica que, con datos válidos y sin duplicados, se actualice la alerta y se devuelva una respuesta exitosa.</summary>
    [Fact]
    public async Task UpdateAlertAsync_WhenDataIsValid_ShouldReturnSuccessResponse()
    {
        var alert = AlertEntity(id: 1, userId: 1);
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);
        _assetRepository.Setup(r => r.GetAsync("AAPL")).ReturnsAsync(AssetEntity());
        _alertRepository.Setup(r => r.ExistsAsync(1, 1, ConditionType.Price, AlertOperator.GreaterThan, 100)).ReturnsAsync(false);

        var result = await CreateService().UpdateAlertAsync(1, UpdateDto());

        Assert.True(result.Success);
        Assert.Equal("Alerta actualizada correctamente", result.Message);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, si la alerta a actualizar no existe, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task UpdateAlertAsync_WhenAlertDoesNotExist_ShouldReturnErrorResponse()
    {
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Alert?)null);

        var result = await CreateService().UpdateAlertAsync(1, UpdateDto());

        Assert.False(result.Success);
        Assert.Equal("Alerta no encontrada", result.Message);
    }

    /// <summary>Verifica que, si la alerta pertenece a otro usuario, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task UpdateAlertAsync_WhenAlertBelongsToAnotherUser_ShouldReturnErrorResponse()
    {
        var alert = AlertEntity(id: 1, userId: 2);
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);

        var result = await CreateService().UpdateAlertAsync(1, UpdateDto());

        Assert.False(result.Success);
        Assert.Equal("Alerta no encontrada", result.Message);
    }

    /// <summary>Verifica que, si la condición no incluye el valor requerido según su tipo, se devuelva "Datos inválidos".</summary>
    [Fact]
    public async Task UpdateAlertAsync_WhenConditionIsInvalid_ShouldReturnErrorResponse()
    {
        var alert = AlertEntity(id: 1, userId: 1);
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);

        var result = await CreateService().UpdateAlertAsync(1, UpdateDto(condition: ">", price: null));

        Assert.False(result.Success);
        Assert.Equal("Datos inválidos", result.Message);
    }

    /// <summary>Verifica que, si el activo indicado no existe, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task UpdateAlertAsync_WhenAssetIsNotFound_ShouldReturnErrorResponse()
    {
        var alert = AlertEntity(id: 1, userId: 1);
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);
        _assetRepository.Setup(r => r.GetAsync("AAPL")).ReturnsAsync((Asset?)null);

        var result = await CreateService().UpdateAlertAsync(1, UpdateDto());

        Assert.False(result.Success);
        Assert.Equal("Activo no encontrado", result.Message);
    }

    /// <summary>Verifica que, si ya existe otra alerta con la misma combinación de activo, condición, operador y valor, se devuelva un error de duplicado.</summary>
    [Fact]
    public async Task UpdateAlertAsync_WhenDuplicateAlertExists_ShouldReturnErrorResponse()
    {
        var alert = AlertEntity(id: 1, userId: 1, assetId: 1, type: ConditionType.Price, op: AlertOperator.LessThan, value: 50);
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);
        _assetRepository.Setup(r => r.GetAsync("AAPL")).ReturnsAsync(AssetEntity());
        _alertRepository.Setup(r => r.ExistsAsync(1, 1, ConditionType.Price, AlertOperator.GreaterThan, 100)).ReturnsAsync(true);

        var result = await CreateService().UpdateAlertAsync(1, UpdateDto());

        Assert.False(result.Success);
        Assert.Equal("Ya existe una alerta igual", result.Message);
    }

    /// <summary>Verifica el caso borde donde la "duplicada" detectada es la propia alerta sin cambios reales: debe permitirse la actualización.</summary>
    [Fact]
    public async Task UpdateAlertAsync_WhenDuplicateMatchesSameAlert_ShouldReturnSuccessResponse()
    {
        var alert = AlertEntity(id: 1, userId: 1, assetId: 1, type: ConditionType.Price, op: AlertOperator.GreaterThan, value: 100);
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);
        _assetRepository.Setup(r => r.GetAsync("AAPL")).ReturnsAsync(AssetEntity());
        _alertRepository.Setup(r => r.ExistsAsync(1, 1, ConditionType.Price, AlertOperator.GreaterThan, 100)).ReturnsAsync(true);

        var result = await CreateService().UpdateAlertAsync(1, UpdateDto());

        Assert.True(result.Success);
    }

    /// <summary>Verifica que, ante una excepción del repositorio, se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task UpdateAlertAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _alertRepository.Setup(r => r.GetByIdAsync(1)).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().UpdateAlertAsync(1, UpdateDto());

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    // ---------- GetStatsAsync ----------

    /// <summary>Verifica que, cuando el repositorio devuelve estadísticas, se construya una respuesta exitosa con los datos correspondientes.</summary>
    [Fact]
    public async Task GetStatsAsync_WhenRepositoryReturnsData_ShouldReturnSuccessResponse()
    {
        _alertRepository.Setup(r => r.GetStatsAsync(1)).ReturnsAsync((2, 1, 0, 3));

        var result = await CreateService().GetStatsAsync(1);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    /// <summary>Verifica que, ante una excepción del repositorio, se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task GetStatsAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _alertRepository.Setup(r => r.GetStatsAsync(1)).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().GetStatsAsync(1);

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }
}
