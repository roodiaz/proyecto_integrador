using InvestLab.Business.Interfaces.Api;
using InvestLab.Models;
using InvestLab.Models.DTOs.Contact;
using InvestLab.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class ContactService : IContactService
{
    private readonly IEmailService _emailService;
    private readonly ContactOptions _contactOptions;
    private readonly ILogger<ContactService> _logger;

    public ContactService(
        IEmailService emailService,
        IOptions<ContactOptions> contactOptions,
        ILogger<ContactService> logger)
    {
        _emailService = emailService;
        _contactOptions = contactOptions.Value;
        _logger = logger;
    }

    public async Task<Response> SendAsync(ContactMessageDto dto)
    {
        try
        {
            await _emailService.SendAsync(_contactOptions.Email,
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