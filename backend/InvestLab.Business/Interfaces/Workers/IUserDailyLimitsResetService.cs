namespace InvestLab.Business.Interfaces.Workers;

public interface IUserDailyLimitsResetService
{
    Task ResetDailyLimitsAsync();
}