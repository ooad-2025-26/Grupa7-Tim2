namespace TimeForPill.Services
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string From { get; set; } = "noreply@timeforpill.local";
        public string DoctorEmail { get; set; } = "ahakanovic1@etf.unsa.ba";
        public string PatientEmail { get; set; } = "ehamidovic1@etf.unsa.ba";
        public string ContactEmail { get; set; } = "ebosnjakov2@etf.unsa.ba";
    }
}
