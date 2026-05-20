using InvestLab.Business.Interfaces.Workers;
using InvestLab.Data.Interfaces;

namespace InvestLab.Business.Services.Workers;

/// <summary>
/// Servicio encargado de resetear
/// límites diarios de usuarios.
/// </summary>
public class UserDailyLimitsResetService : IUserDailyLimitsResetService
{
    private readonly IUserSettingRepository _repository;

    public UserDailyLimitsResetService(IUserSettingRepository repository)
    {
        _repository = repository;
    }

    public async Task ResetDailyLimitsAsync()
    {
        await _repository.ResetDailyLimitsAsync();
    }
}