public static class EmailTemplates
{
    private const string BrandAccentColor = "#527bd9";

    /// <summary>
    /// Envuelve el contenido de un correo (el fragmento HTML propio de cada plantilla)
    /// con el layout compartido de InvestLab: wordmark, tarjeta con barra de acento y footer.
    /// </summary>
    /// <param name="content">HTML interno del correo (sin &lt;html&gt;/&lt;body&gt;).</param>
    /// <param name="accentColor">Color de la barra superior de la tarjeta (por defecto, el azul de marca).</param>
    /// <returns>El HTML completo del correo, listo para enviar.</returns>
    private static string WrapInLayout(string content, string accentColor = BrandAccentColor)
    {
        var layoutPath = Path.Combine(AppContext.BaseDirectory, "Templates", "EmailLayout.html");
        var layout = File.ReadAllText(layoutPath);

        return layout
            .Replace("{{CONTENT}}", content)
            .Replace("{{ACCENT_COLOR}}", accentColor);
    }

    public static string VerificationCode(string code)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Templates",
            "VerificationCode.html");

        var content = File.ReadAllText(path).Replace("{{CODE}}", code);

        return WrapInLayout(content);
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

        var accentColor = trend switch
        {
            "up" => "#16a34a",
            "down" => "#dc2626",
            _ => BrandAccentColor
        };

        var trendIcon = trend switch
        {
            "up" => "📈",
            "down" => "📉",
            _ => "🔔"
        };

        var content = File.ReadAllText(path)
            .Replace("{{SYMBOL}}", symbol)
            .Replace("{{MESSAGE}}", message)
            .Replace("{{PRICE}}", price.ToString("N2"))
            .Replace("{{DATE}}", triggeredAt.ToString("dd/MM/yyyy HH:mm") + " UTC")
            .Replace("{{ACCENT_COLOR}}", accentColor)
            .Replace("{{TREND_ICON}}", trendIcon);

        return WrapInLayout(content, accentColor);
    }
}