using InvestLab.Business.Interfaces.Api;
using InvestLab.Models.DTOs.Market;

namespace InvestLab.Business.Services.Api
{
    /// <summary>
    /// Servicio centralizado que determina el estado del mercado (abierto o cerrado)
    /// en función del horario regular de Wall Street (sin contemplar pre-market ni after-hours).
    /// </summary>
    public class MarketStatusService : IMarketStatusService
    {
        /// <summary>
        /// Determina el estado actual del mercado (abierto o cerrado) en función del horario y día de la semana de la zona horaria del Este de Estados Unidos.
        /// </summary>
        /// <returns>Un objeto con el estado del mercado, el texto descriptivo, la hora actual del mercado y los horarios de apertura y cierre.</returns>
        public MarketStatusDto GetStatus()
        {
            var timeZone = GetEasternTimeZone();
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            var isOpen = IsOpen(now);

            return new MarketStatusDto
            {
                IsOpen = isOpen,
                StatusText = isOpen ? "Mercado abierto" : "Mercado cerrado",
                MarketTime = now.ToString("HH:mm:ss"),
                TimeZone = "America/New_York",
                OpenTime = "09:30",
                CloseTime = "16:00"
            };
        }

        /// <summary>
        /// Indica si el mercado se encuentra actualmente abierto, según el horario regular (sin pre-market ni after-hours).
        /// </summary>
        /// <returns><c>true</c> si el mercado está abierto; en caso contrario, <c>false</c>.</returns>
        public bool IsMarketOpen()
        {
            var timeZone = GetEasternTimeZone();
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            return IsOpen(now);
        }

        /// <summary>
        /// Determina si una fecha y hora dadas (en la zona horaria del Este de Estados Unidos) corresponden al horario regular de mercado.
        /// </summary>
        /// <param name="easternTime">Fecha y hora en la zona horaria del Este de Estados Unidos.</param>
        /// <returns><c>true</c> si corresponde a un día hábil dentro del horario 09:30-16:00; en caso contrario, <c>false</c>.</returns>
        private static bool IsOpen(DateTime easternTime)
        {
            var open = new TimeSpan(9, 30, 0);
            var close = new TimeSpan(16, 0, 0);
            var isBusinessDay = easternTime.DayOfWeek != DayOfWeek.Saturday && easternTime.DayOfWeek != DayOfWeek.Sunday;

            return isBusinessDay && easternTime.TimeOfDay >= open && easternTime.TimeOfDay <= close;
        }

        /// <summary>
        /// Obtiene la información de la zona horaria del Este de Estados Unidos, contemplando los distintos identificadores según el sistema operativo.
        /// </summary>
        /// <returns>La zona horaria correspondiente a "America/New_York" o, si no está disponible, a "Eastern Standard Time".</returns>
        private static TimeZoneInfo GetEasternTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            }
            catch
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            }
        }
    }
}
