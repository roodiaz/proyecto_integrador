using InvestLab.Data;

public interface IJwtService
{
    Task<TokenDto> GenerateTokensAsync(User user);
}