using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace WindowsServiceManager
{
    public sealed partial class MainWindow : Window
    {
        private ObservableCollection<ServiceModel> AllServices = new();
        private ObservableCollection<ServiceModel> FilteredServices = new();

        public MainWindow()
        {
            this.InitializeComponent();
            _ = LoadServicesAsync();
        }

        private async Task LoadServicesAsync()
        {
            ServiceListView.ItemsSource = null;
            var list = await Task.Run(() =>
            {
                var tmp = new List<ServiceModel>();
                var wmi = FetchWmiServiceInfo();
                foreach (var sc in ServiceController.GetServices())
                {
                    using (sc)
                    {
                        wmi.TryGetValue(sc.ServiceName, out var info);
                        tmp.Add(new ServiceModel
                        {
                            ServiceName = sc.ServiceName,
                            Description = sc.DisplayName,
                            Status = sc.Status.ToString(),
                            StartupType = info.StartupType ?? "Unknown",
                            LogOnAs = info.LogOnAs ?? "Unknown"
                        });
                    }
                }
                return tmp;
            });

            AllServices = new ObservableCollection<ServiceModel>(list);
            FilteredServices = new ObservableCollection<ServiceModel>(list);
            ServiceListView.ItemsSource = FilteredServices;
        }

        private Dictionary<string, (string StartupType, string LogOnAs)> FetchWmiServiceInfo()
        {
            var dict = new Dictionary<string, (string, string)>();
            try
            {
                var qs = new ManagementObjectSearcher("SELECT Name,StartMode,StartName FROM Win32_Service");
                foreach (ManagementObject mo in qs.Get())
                {
                    var name = mo["Name"]?.ToString() ?? "";
                    var mode = mo["StartMode"]?.ToString() ?? "Unknown";
                    var logon = mo["StartName"]?.ToString() ?? "Unknown";
                    dict[name] = (mode, logon);
                }
            }
            catch { }
            return dict;
        }

        private async void RefreshServices_Click(object sender, RoutedEventArgs e) =>
            await LoadServicesAsync();

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text?.Trim().ToLower() ?? "";
            FilteredServices = new ObservableCollection<ServiceModel>(
                AllServices.Where(s => s.ServiceName?.ToLower().Contains(q) == true));
            ServiceListView.ItemsSource = FilteredServices;
        }

        private async void StartService_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceListView.SelectedItem is ServiceModel m)
            {
                var success = await RunServiceCommandAsync($"sc start \"{m.ServiceName}\"");
                if (!success)
                    await ShowMessageAsync("Failed to start service. Try running the app as Administrator.");
                await LoadServicesAsync();
            }
        }

        private async void StopService_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceListView.SelectedItem is ServiceModel m)
            {
                var success = await RunServiceCommandAsync($"sc stop \"{m.ServiceName}\"");
                if (!success)
                    await ShowMessageAsync("Failed to stop service. Try running the app as Administrator.");
                await LoadServicesAsync();
            }
        }

        private async void PauseService_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceListView.SelectedItem is ServiceModel m)
            {
                var success = await RunServiceCommandAsync($"sc pause \"{m.ServiceName}\"");
                if (!success)
                    await ShowMessageAsync("Failed to pause service. Try running the app as Administrator.");
                await LoadServicesAsync();
            }
        }

        private async void Status_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is TextBlock tb && tb.DataContext is ServiceModel svc)
            {
                var menu = new MenuFlyout();

                var start = new MenuFlyoutItem { Text = "Start" };
                start.Click += async (_, __) =>
                {
                    await RunServiceCommandAsync($"sc start \"{svc.ServiceName}\"");
                    await LoadServicesAsync();
                };
                menu.Items.Add(start);

                var stop = new MenuFlyoutItem { Text = "Stop" };
                stop.Click += async (_, __) =>
                {
                    await RunServiceCommandAsync($"sc stop \"{svc.ServiceName}\"");
                    await LoadServicesAsync();
                };
                menu.Items.Add(stop);

                var pause = new MenuFlyoutItem { Text = "Pause" };
                pause.Click += async (_, __) =>
                {
                    await RunServiceCommandAsync($"sc pause \"{svc.ServiceName}\"");
                    await LoadServicesAsync();
                };
                menu.Items.Add(pause);

                menu.ShowAt(tb, new FlyoutShowOptions { Position = e.GetPosition(tb) });
            }
        }

        private async void StartupType_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is TextBlock tb && tb.DataContext is ServiceModel svc)
            {
                var dlg = new ContentDialog
                {
                    Title = $"Change Startup Type — {svc.ServiceName}",
                    CloseButtonText = "Cancel",
                    PrimaryButtonText = "Apply",
                    XamlRoot = this.Content.XamlRoot
                };
                var opts = new List<string> { "Automatic", "Manual", "Disabled" };
                var combo = new ComboBox
                {
                    ItemsSource = opts,
                    SelectedItem = svc.StartupType,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                dlg.Content = combo;
                if (await dlg.ShowAsync() == ContentDialogResult.Primary)
                {
                    var newType = combo.SelectedItem as string;
                    if (!string.IsNullOrWhiteSpace(newType) && newType != svc.StartupType)
                    {
                        if (await ChangeStartupTypeAsync(svc.ServiceName!, newType))
                            await LoadServicesAsync();
                        else
                            await ShowMessageAsync("Failed: run app as Administrator.");
                    }
                }
            }
        }

        private async Task<bool> ChangeStartupTypeAsync(string serviceName, string? startupType)
        {
            if (string.IsNullOrWhiteSpace(startupType)) return false;
            var mode = startupType switch
            {
                "Automatic" => "auto",
                "Manual" => "demand",
                "Disabled" => "disabled",
                _ => null
            };
            if (mode is null) return false;

            var cmd = $"sc config \"{serviceName}\" start= {mode}";
            return await RunServiceCommandAsync(cmd);
        }

        private async Task<bool> RunServiceCommandAsync(string command)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                Verb = "runas", // Ensure admin rights
                UseShellExecute = true,
                CreateNoWindow = true
            };

            try
            {
                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null) return false;
                await proc.WaitForExitAsync();
                return proc.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task ShowMessageAsync(string msg)
        {
            var dlg = new ContentDialog
            {
                Title = "Notification",
                Content = msg,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dlg.ShowAsync();
        }
    }
}
