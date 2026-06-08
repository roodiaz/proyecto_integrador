using InvestLab.Models;
using InvestLab.Models.DTOs.Auth;

public interface IAuthService
{
    Task<Response> RegisterAsync(RegisterDto registerDto);
    Task<Response> VerifyAsync(VerifyDto dto);
    Task<Response> ResendCodeAsync(ResendCodeDto dto);
    Task<Response> LoginAsync(LoginDto dto);
    Task<Response> RefreshTokenAsync(string refreshToken);
    Task<Response> LogoutAsync(string refreshToken);
    Task<Response> ForgotPasswordAsync(ForgotPasswordDto dto);
    Task<Response> ResetPasswordAsync(ResetPasswordDto dto);

}