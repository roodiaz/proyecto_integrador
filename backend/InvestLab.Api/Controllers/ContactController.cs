using InvestLab.Api.Extensions;
using InvestLab.Business.Interfaces.Api;
using InvestLab.Models;
using InvestLab.Models.DTOs.Contact;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvestLab.Api.Controllers;

/// <summary>
/// Controlador público encargado de recibir mensajes
/// del formulario de contacto del sitio.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ContactController : ControllerBase
{
    private readonly IContactService _service;
    private readonly ILogger<ContactController> _logger;

    public ContactController(IContactService service, ILogger<ContactController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Envía un mensaje desde el formulario
    /// de contacto del sitio.
    /// </summary>
    /// <param name="dto">
    /// Información ingresada por el usuario.
    /// </param>
    /// <returns>
    /// Resultado de la operación.
    /// </returns>
    /// <response code="200">
    /// Mensaje enviado correctamente.
    /// </response>
    /// <response code="400">
    /// Error al procesar la solicitud.
    /// </response>
    [EnableRateLimiting(RateLimitPolicies.Contact)]
    [HttpPost("contact")]
    [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Send([FromBody] ContactMessageDto dto)
    {
        _logger.LogInformation("Solicitud de contacto recibida desde {Email}", dto.Email);

        var result = await _service.SendAsync(dto);

        if (!result.Success)
        {
            _logger.LogWarning("Error al enviar contacto: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }
}