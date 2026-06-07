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

    /// <summary>
    /// Inicializa una nueva instancia del servicio de reseteo de límites diarios de usuarios.
    /// </summary>
    /// <param name="repository">Repositorio de configuraciones de usuario utilizado para acceder a los datos persistidos.</param>
    public UserDailyLimitsResetService(IUserSettingRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Resetea de forma asíncrona los límites diarios de todos los usuarios delegando la operación al repositorio correspondiente.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona de reseteo.</returns>
    public async Task ResetDailyLimitsAsync()
    {
        await _repository.ResetDailyLimitsAsync();
    }
}