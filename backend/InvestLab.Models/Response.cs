namespace InvestLab.Models;
public class Response
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }

    public static Response Ok(object? data = null, string message = "")
    {
        return new Response
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static Response Fail(string message)
    {
        return new Response
        {
            Success = false,
            Message = message,
            Data = null
        };
    }
}