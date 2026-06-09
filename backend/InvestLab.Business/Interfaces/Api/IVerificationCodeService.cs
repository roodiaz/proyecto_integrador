using InvestLab.Data;

namespace InvestLab.Business.Interfaces.Api
{
    /// <summary>
    /// Resultado de la validación de un código de verificación temporal.
    /// </summary>
    public class VerificationCodeResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public UserTempCredential? Credential { get; set; }

        public static VerificationCodeResult Fail(string message, string? code = null) =>
            new() { Success = false, ErrorMessage = message, ErrorCode = code };
        public static VerificationCodeResult Ok(UserTempCredential credential) =>
            new() { Success = true, Credential = credential };
    }

    /// <summary>
    /// Servicio reutilizable para generar, enviar y validar códigos de verificación temporales
    /// (utilizados durante el registro de cuenta, el cambio de email y futuras verificaciones).
    /// </summary>
    public interface IVerificationCodeService
    {
        /// <summary>
        /// Genera un nuevo código de verificación, lo almacena (reemplazando el anterior si existe)
        /// y lo envía por correo electrónico.
        /// </summary>
        /// <param name="user">Usuario propietario de la credencial temporal.</param>
        /// <param name="emailSubject">Asunto del correo de verificación a enviar.</param>
        /// <param name="pendingEmail">
        /// Email de destino pendiente de confirmación (por ejemplo, durante un cambio de email).
        /// Si se especifica, el código se envía a esta dirección y queda asociado a la credencial
        /// para poder aplicarse una vez validado. Si es <c>null</c>, el código se envía al email actual del usuario.
        /// </param>
        /// <returns><c>true</c> si el correo fue enviado correctamente; en caso contrario, <c>false</c>.</returns>
        Task<bool> GenerateAndSendCodeAsync(User user, string emailSubject, string? pendingEmail = null);

        /// <summary>
        /// Valida un código de verificación ingresado por el usuario contra la credencial temporal almacenada.
        /// </summary>
        /// <param name="user">Usuario propietario de la credencial temporal.</param>
        /// <param name="code">Código ingresado a validar.</param>
        /// <returns>
        /// Un <see cref="VerificationCodeResult"/> indicando si la validación fue exitosa (incluyendo la
        /// credencial encontrada) o el motivo del fallo.
        /// </returns>
        Task<VerificationCodeResult> ValidateCodeAsync(User user, string code);
    }
}
