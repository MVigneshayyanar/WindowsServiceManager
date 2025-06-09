using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.ServiceProcess;

namespace WindowsServiceManager
{
    public sealed partial class MainWindow : Window
    {
        private ObservableCollection<ServiceModel> Services { get; set; } = new();

        public MainWindow()
        {
            this.InitializeComponent();
            LoadServices();
        }

        private void LoadServices()
        {
            Services.Clear();

            try
            {
                ServiceController[] scServices = ServiceController.GetServices();

                foreach (var service in scServices)
                {
                    Services.Add(new ServiceModel
                    {
                        ServiceName = service.ServiceName,
                        Description = service.DisplayName,
                        Status = service.Status.ToString(),
                        StartupType = "N/A", // requires advanced API to fetch
                        LogOnAs = "N/A"      // requires advanced API to fetch
                    });
                }

                ServiceListView.ItemsSource = Services;
            }
            catch
            {
                // Handle error appropriately (permissions, etc.)
            }
        }

        private void RefreshServices_Click(object sender, RoutedEventArgs e)
        {
            LoadServices();
        }
    }
}
