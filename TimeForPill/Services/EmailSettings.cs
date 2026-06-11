namespace TimeForPill.Services
{
    public class EmailSettings
    {
        public const string PublicAppUrl = "http://timeforpill.runasp.net";
        private const string PublicAppHost = "timeforpill.runasp.net";

        public string Host { get; set; } = string.Empty;
        public string SmtpServer { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string UserName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string From { get; set; } = "noreply@timeforpill.local";
        public string SenderName { get; set; } = "TimeForPill";
        public string SenderEmail { get; set; } = string.Empty;
        public string AppUrl { get; set; } = PublicAppUrl;

        public string EffectiveHost =>
            FirstConfigured(SmtpServer, Host);

        public string EffectiveUserName =>
            FirstConfigured(Username, UserName, SenderEmail);

        public string EffectiveFrom =>
            FirstConfigured(SenderEmail, From, UserName, Username);

        public string EffectiveAppUrl
        {
            get
            {
                var appUrl = string.IsNullOrWhiteSpace(AppUrl)
                    ? PublicAppUrl
                    : AppUrl.Trim();

                if (!appUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !appUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    appUrl = "http://" + appUrl;
                }

                if (Uri.TryCreate(appUrl, UriKind.Absolute, out var uri) &&
                    string.Equals(uri.Host, PublicAppHost, StringComparison.OrdinalIgnoreCase))
                {
                    return PublicAppUrl;
                }

                return appUrl.TrimEnd('/');
            }
        }

        private static string FirstConfigured(params string?[] values)
        {
            return values.FirstOrDefault(value =>
                !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }
    }
}
