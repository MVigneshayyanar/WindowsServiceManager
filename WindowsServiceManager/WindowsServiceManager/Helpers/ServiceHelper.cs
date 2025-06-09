using System.Management;

namespace WindowsServiceManager.Helpers
{
    public static class ServiceHelper
    {
        public static (string StartupType, string LogOnAs) GetServiceDetails(string serviceName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT StartMode, StartName FROM Win32_Service WHERE Name='{serviceName}'");
                foreach (var obj in searcher.Get())
                {
                    return (obj["StartMode"]?.ToString() ?? "--", obj["StartName"]?.ToString() ?? "--");
                }
            }
            catch
            {
                // ignore
            }
            return ("--", "--");
        }
    }
}
