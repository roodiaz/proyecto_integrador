using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.DTOs.Contact;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace InvestLab.Tests.Business.Services.Api;

/// <summary>
/// Pruebas unitarias de <see cref="ContactService"/>, cubriendo el método invocado desde <c>ContactController</c>
/// para el envío de mensajes del formulario de contacto.
/// </summary>
public class ContactServiceTests
{
    private readonly Mock<IEmailProvider> _emailProvider = new();
    private readonly Mock<IEmailProviderResolver> _emailProviderResolver = new();
    private readonly Mock<ILogger<ContactService>> _logger = new();
    private readonly EmailOptions _options = new() { Gmail = new GmailOptions { Username = "soporte@investlab.com" } };

    private ContactService CreateService()
    {
        _emailProviderResolver.Setup(x => x.GetProvider()).Returns(_emailProvider.Object);
        return new(_emailProviderResolver.Object, Options.Create(_options), _logger.Object);
    }

    private static ContactMessageDto Dto(string name = "Juan", string email = "juan@test.com", string? phone = "123", string message = "Hola, tengo una consulta") =>
        new() { Name = name, Email = email, Phone = phone, Message = message };

    /// <summary>Verifica que, con datos válidos, el mensaje se envíe a la cuenta de Gmail configurada y se devuelva una respuesta exitosa.</summary>
    [Fact]
    public async Task SendAsync_WhenDataIsValid_ShouldSendEmailToConfiguredAddressAndReturnSuccessResponse()
    {
        var dto = Dto();

        var result = await CreateService().SendAsync(dto);

        Assert.True(result.Success);
        Assert.Equal("Mensaje enviado correctamente", result.Message);
        _emailProvider.Verify(e => e.SendAsync(_options.Gmail.Username, It.Is<string>(s => s.Contains(dto.Name)), It.Is<string>(b => b.Contains(dto.Email) && b.Contains(dto.Message))), Times.Once);
    }

    /// <summary>Verifica que, ante un error del proveedor de email, se registre la excepción y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task SendAsync_WhenEmailServiceThrows_ShouldReturnErrorResponse()
    {
        _emailProvider.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("smtp error"));

        var result = await CreateService().SendAsync(Dto());

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    /// <summary>Verifica el caso borde donde el teléfono no fue proporcionado: el mensaje igualmente debe enviarse correctamente.</summary>
    [Fact]
    public async Task SendAsync_WhenPhoneIsNotProvided_ShouldStillSendEmailAndReturnSuccessResponse()
    {
        var dto = Dto(phone: null);

        var result = await CreateService().SendAsync(dto);

        Assert.True(result.Success);
        _emailProvider.Verify(e => e.SendAsync(_options.Gmail.Username, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
