namespace InvestLab.Models;
public class Response
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
    public object? Data { get; set; }

    public static Response Ok(object? data = null, string message = "", string? code = null)
    {
        return new Response
        {
            Success = true,
            Message = message,
            Code = code,
            Data = data
        };
    }

    public static Response Fail(string message, string? code = null, object? data = null)
    {
        return new Response
        {
            Success = false,
            Message = message,
            Code = code,
            Data = data
        };
    }
}