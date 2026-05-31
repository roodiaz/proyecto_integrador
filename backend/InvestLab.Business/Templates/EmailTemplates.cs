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
}