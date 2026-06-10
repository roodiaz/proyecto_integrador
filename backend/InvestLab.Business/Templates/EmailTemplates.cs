public static class EmailTemplates
{
    public static string VerificationCode(string code)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Templates",
            "VerificationCode.html");

        var html = File.ReadAllText(path);

        return html.Replace("{{CODE}}", code);
    }

    /// <summary>
    /// Genera el HTML del correo enviado cuando se activa una alerta de precio o variación de un activo.
    /// </summary>
    /// <param name="symbol">Símbolo del activo cuya alerta se activó.</param>
    /// <param name="message">Mensaje descriptivo de la alerta activada.</param>
    /// <param name="price">Precio de mercado actual del activo al momento de activarse la alerta.</param>
    /// <param name="triggeredAt">Fecha y hora (UTC) en que se activó la alerta.</param>
    /// <param name="trend">Tendencia asociada a la alerta ("up", "down" o cualquier otro valor para neutral), usada para definir el color y el ícono del correo.</param>
    /// <returns>El HTML del correo con los datos de la alerta reemplazados en la plantilla.</returns>
    public static string AlertTriggered(string symbol, string message, decimal price, DateTime triggeredAt, string trend)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Templates",
            "AlertTriggered.html");

        var html = File.ReadAllText(path);

        var accentColor = trend switch
        {
            "up" => "#16a34a",
            "down" => "#dc2626",
            _ => "#4d8eff"
        };

        var trendIcon = trend switch
        {
            "up" => "📈",
            "down" => "📉",
            _ => "🔔"
        };

        return html
            .Replace("{{SYMBOL}}", symbol)
            .Replace("{{MESSAGE}}", message)
            .Replace("{{PRICE}}", price.ToString("N2"))
            .Replace("{{DATE}}", triggeredAt.ToString("dd/MM/yyyy HH:mm") + " UTC")
            .Replace("{{ACCENT_COLOR}}", accentColor)
            .Replace("{{TREND_ICON}}", trendIcon);
    }
}