using InvestLab.Business.Interfaces.Api;
using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Contact;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class ContactService : IContactService
{
    private readonly IEmailProviderResolver _emailProviderResolver;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<ContactService> _logger;

    /// <summary>
    /// Inicializa una nueva instancia del servicio de contacto con sus dependencias.
    /// </summary>
    /// <param name="emailProviderResolver">Resolver utilizado para obtener el proveedor de envío de correos electrónicos activo.</param>
    /// <param name="emailOptions">Opciones de configuración de correo electrónico, utilizadas para determinar la cuenta destino del formulario de contacto.</param>
    /// <param name="logger">Registrador de eventos para el servicio de contacto.</param>
    public ContactService(
        IEmailProviderResolver emailProviderResolver,
        IOptions<EmailOptions> emailOptions,
        ILogger<ContactService> logger)
    {
        _emailProviderResolver = emailProviderResolver;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Envía un mensaje de contacto por correo electrónico a la cuenta de correo configurada del sistema.
    /// </summary>
    /// <param name="dto">Datos del mensaje de contacto, incluyendo nombre, email, teléfono y mensaje del remitente.</param>
    /// <returns>Una respuesta indicando si el mensaje fue enviado correctamente o si ocurrió un error interno.</returns>
    public async Task<Response> SendAsync(ContactMessageDto dto)
    {
        try
        {
            await _emailProviderResolver.GetProvider().SendAsync(_emailOptions.Gmail.Username,
                $"Nuevo mensaje de contacto - {dto.Name}",
                $"""
                    <h2>Nuevo mensaje de contacto</h2>

                    <hr>

                    <p><strong>Nombre:</strong> {dto.Name}</p>
                    <p><strong>Email:</strong> {dto.Email}</p>
                    <p><strong>Teléfono:</strong> {dto.Phone}</p>

                    <p><strong>Mensaje:</strong></p>

                    <p>{dto.Message}</p>
                """
            );

            _logger.LogInformation("Mensaje de contacto recibido desde {Email}", dto.Email);

            return Response.Ok(null, "Mensaje enviado correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar mensaje de contacto");
            return Response.Fail("Error interno");
        }
    }
}
