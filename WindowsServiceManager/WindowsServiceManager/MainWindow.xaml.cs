using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace WindowsServiceManager
{
    public sealed partial class MainWindow : Window
    {
        private ObservableCollection<ServiceModel> AllServices { get; set; } = new();
        private ObservableCollection<ServiceModel> FilteredServices { get; set; } = new();

        public MainWindow()
        {
            this.InitializeComponent();
            _ = LoadServicesAsync();  // Load services on startup asynchronously
        }

        private async Task LoadServicesAsync()
        {
            ServiceListView.ItemsSource = null; // Clear current list while loading

            var services = await Task.Run(() =>
            {
                var allServices = new List<ServiceModel>();
                var wmiInfo = FetchWmiServiceInfo();

                foreach (var sc in ServiceController.GetServices())
                {
                    wmiInfo.TryGetValue(sc.ServiceName, out var info);

                    allServices.Add(new ServiceModel
                    {
                        ServiceName = sc.ServiceName,
                        Description = sc.DisplayName,
                        Status = sc.Status.ToString(),
                        StartupType = info.StartupType ?? "Unknown",
                        LogOnAs = info.LogOnAs ?? "Unknown"
                    });
                }

                return allServices;
            });

            AllServices = new ObservableCollection<ServiceModel>(services);
            FilteredServices = new ObservableCollection<ServiceModel>(services);

            ServiceListView.ItemsSource = FilteredServices;
        }

        private Dictionary<string, (string StartupType, string LogOnAs)> FetchWmiServiceInfo()
        {
            var dict = new Dictionary<string, (string StartupType, string LogOnAs)>();

            try
            {
                var searcher = new ManagementObjectSearcher("SELECT Name, StartMode, StartName FROM Win32_Service");

                foreach (ManagementObject mo in searcher.Get())
                {
                    string name = mo["Name"]?.ToString() ?? "";
                    string startMode = mo["StartMode"]?.ToString() ?? "Unknown";
                    string startName = mo["StartName"]?.ToString() ?? "Unknown";

                    dict[name] = (startMode, startName);
                }
            }
            catch
            {
                // You can add logging here if needed
            }

            return dict;
        }

        private void RefreshServices_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadServicesAsync();  // Reload list asynchronously on refresh click
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = SearchBox.Text?.Trim().ToLower() ?? "";
            var filtered = AllServices.Where(s => s.ServiceName.ToLower().Contains(query));
            FilteredServices = new ObservableCollection<ServiceModel>(filtered);
            ServiceListView.ItemsSource = FilteredServices;
        }

        private void StartService_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceListView.SelectedItem is ServiceModel model)
            {
                try
                {
                    var sc = new ServiceController(model.ServiceName);
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, System.TimeSpan.FromSeconds(10));
                        _ = LoadServicesAsync();
                    }
                }
                catch
                {
                    // Handle or log exceptions as needed
                }
            }
        }

        private void StopService_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceListView.SelectedItem is ServiceModel model)
            {
                try
                {
                    var sc = new ServiceController(model.ServiceName);
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, System.TimeSpan.FromSeconds(10));
                        _ = LoadServicesAsync();
                    }
                }
                catch
                {
                    // Handle or log exceptions as needed
                }
            }
        }

        private void PauseService_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceListView.SelectedItem is ServiceModel model)
            {
                try
                {
                    var sc = new ServiceController(model.ServiceName);
                    if (sc.CanPauseAndContinue && sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Pause();
                        sc.WaitForStatus(ServiceControllerStatus.Paused, System.TimeSpan.FromSeconds(10));
                        _ = LoadServicesAsync();
                    }
                }
                catch
                {
                    // Handle or log exceptions as needed
                }
            }
        }
    }
}
