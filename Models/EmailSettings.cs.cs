namespace Pi_Odonto.Models
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "";
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; } = "";
        public string SmtpPassword { get; set; } = "";
        public bool EnableSsl { get; set; } = true;
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "";
        public string BaseUrl { get; set; } = "";
    }
}