using InvestLab.Models.DTOs.User.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InvestLab.Business.Interfaces;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    }

    // Endpoint para obtener el perfil del usuario
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _userService.GetProfileAsync(GetUserId());
        return Ok(result);
    }

    // Endpoint para actualizar el perfil del usuario
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.UpdateProfileAsync(GetUserId(), dto);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // Endpoint para cambiar la contraseña del usuario
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.ChangePasswordAsync(GetUserId(), dto);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // Endpoint para actualizar la imagen de perfil del usuario
    [HttpPost("profile-image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        var result = await _userService.UploadProfileImageAsync(GetUserId(), file);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}