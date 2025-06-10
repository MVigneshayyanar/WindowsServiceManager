using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace WindowsServiceManager
{
    public class ServiceModel
    {
        public string? ServiceName { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? StartupType { get; set; }
        public string? LogOnAs { get; set; }

        public SolidColorBrush StatusColor
        {
            get
            {
                return Status switch
                {
                    "Running" => new SolidColorBrush(Colors.Green),
                    "Stopped" => new SolidColorBrush(Colors.Red),
                    "Paused" => new SolidColorBrush(Colors.Orange),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
        }
    }
}
