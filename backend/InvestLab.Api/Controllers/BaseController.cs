using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

public abstract class BaseController : ControllerBase
{
    protected int UserId
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(claim))
                throw new UnauthorizedAccessException("Usuario no autenticado");

            return int.Parse(claim);
        }
    }
}