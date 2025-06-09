namespace WindowsServiceManager.Models
{
    public class ServiceModel
    {
        public string ServiceName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StartupType { get; set; } = string.Empty;
        public string LogOnAs { get; set; } = string.Empty;
    }
}
