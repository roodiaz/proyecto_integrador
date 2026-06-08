namespace InvestLab.Integrations.Interfaces;

/// <summary>
/// Resuelve cuál proveedor de envío de correo electrónico debe utilizarse según la configuración.
/// Permite cambiar de proveedor (Gmail, Resend, etc.) sin modificar la lógica de negocio.
/// </summary>
public interface IEmailProviderResolver
{
    IEmailProvider GetProvider();
}
