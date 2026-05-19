public class AuthResponseDto
{
    public bool Success { get; set; }
    public UserDto User { get; set; }
    public TokenDto Tokens { get; set; }
    public string Message { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class TokenDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresIn { get; set; }
}