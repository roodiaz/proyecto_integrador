namespace InvestLab.Integrations.Interfaces;

/// <summary>
/// Resuelve cuál proveedor externo de datos de mercado debe utilizarse según la configuración.
/// Permite cambiar de proveedor (Yahoo, EOD Historical Data, etc.) sin modificar la lógica de negocio.
/// </summary>
public interface IMarketProviderResolver
{
    IExternalProvider GetProvider();
}
