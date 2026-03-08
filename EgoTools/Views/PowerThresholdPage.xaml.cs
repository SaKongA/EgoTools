using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Security.Principal;
using System.Management;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EgoTools.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PowerThresholdPage : Page
    {
        private AppConfig config = default!;
        private int _pendingChargeLimit = 80;
        public PowerThresholdPage()
        {
            InitializeComponent();
            config = App.LoadConfig() ?? new AppConfig
            {
                KeyboardSettings = new KeyboardSettings { KeyboardDetachment = false },
                ColorManagement = new ColorManagement { CurrentMode = "Factory", CurrentProfile = "sRGB", IgcFile = "", _3dlutFile = "" },
                PowerThreshold = new PowerThreshold { ChargeLimit = 80 }
            };
            if (config?.PowerThreshold == null)
            {
                config.PowerThreshold = new PowerThreshold { ChargeLimit = 80 };
            }
            int limit = config?.PowerThreshold?.ChargeLimit ?? 80;
            ThresholdSlider.Value = limit;
            ThresholdValueText.Text = limit.ToString();
            ThresholdSlider.ValueChanged += ThresholdSlider_ValueChanged;
            ThresholdSlider.ManipulationCompleted += ThresholdSlider_ManipulationCompleted;
            UacButton.Click += UacButton_Click;
            this.Loaded += PowerThresholdPage_Loaded;
        }

        private async void PowerThresholdPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!IsAdministrator())
            {
                ThresholdSlider.IsEnabled = false;
                await ShowUacWarningDialog();
            }
            else
            {
                ThresholdSlider.IsEnabled = true;
                // 修改UacButton为对勾+已获取UAC权限，并禁用
                UacButton.Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new FontIcon { Glyph = "\uE73E", FontSize = 16, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.ForestGreen) }, new TextBlock { Text = "已获取UAC权限", Margin = new Thickness(4,0,0,0), VerticalAlignment = VerticalAlignment.Center } } };
                UacButton.IsEnabled = false;
            }
        }

        private bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private async System.Threading.Tasks.Task ShowUacWarningDialog()
        {
            var dialog = new ContentDialog
            {
                RequestedTheme=EgoTools.App.RootTheme,
                Title = "需要管理员权限",
                Content = "请点击上方‘获取’按钮，以管理员身份重启应用后再调整电源阈值。",
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void ThresholdSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            int value = (int)e.NewValue;
            ThresholdValueText.Text = value.ToString();
            _pendingChargeLimit = value;
            if (value <= 60)
                BatteryIcon.Glyph = "\xEBAF";
            else if (value <= 70)
                BatteryIcon.Glyph = "\xEBB0";
            else if (value <= 80)
                BatteryIcon.Glyph = "\xEBB1";
            else if (value <= 90)
                BatteryIcon.Glyph = "\xEBB3";
            else if (value <= 99)
                BatteryIcon.Glyph = "\xEBB4";
            else
                BatteryIcon.Glyph = "\xEBB5";
            if (config != null)
            {
                if (config.PowerThreshold == null)
                    config.PowerThreshold = new PowerThreshold();
                config.PowerThreshold.ChargeLimit = value;
                App.SaveConfig(config);
            }
        }
        private async void ThresholdSlider_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            await SetBatteryThresholdAsync();
        }

        private async Task SetBatteryThresholdAsync()
        {
            var progressBar = new ProgressBar
            {
                IsIndeterminate = true,
                Width = 250,
                Height = 20,
                Margin = new Thickness(0, 16, 0, 0)
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = "正在设置电池阈值...", FontSize = 16, Margin = new Thickness(0,0,0,8) });
            stack.Children.Add(progressBar);
            var runningDialog = new ContentDialog
            {
                RequestedTheme=EgoTools.App.RootTheme,
                Title = "请稍候",
                Content = stack
            };
            runningDialog.XamlRoot = this.XamlRoot;
            var showTask = runningDialog.ShowAsync();
            var minDurationTask = System.Threading.Tasks.Task.Delay(1500);

            try
            {
                int percentageLimit = Math.Max(50, Math.Min(100, _pendingChargeLimit));
                byte[] request = new byte[64];
                request[0] = 0x03;
                request[1] = 0x15;
                request[2] = 0x01;
                request[3] = 0x18;
                request[4] = (byte)(percentageLimit - 5);
                request[5] = (byte)percentageLimit;

                var scope = new ManagementScope(@"\\.\ROOT\WMI");
                var query = new ObjectQuery("SELECT * FROM OemWMIMethod");
                using (var searcher = new ManagementObjectSearcher(scope, query))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        obj.InvokeMethod("OemWMIfun", new object[] { request });
                        break; // 只需调用一次
                    }
                }
                await minDurationTask;
                runningDialog.Hide();
            }
            catch (Exception ex)
            {
                await minDurationTask;
                runningDialog.Hide();
                var dialog = new ContentDialog
                {
                    RequestedTheme=EgoTools.App.RootTheme,
                    Title = "设置失败",
                    Content = $"调用WMI失败：{ex.Message}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
        private async void UacButton_Click(object sender, RoutedEventArgs e)
        {
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas"
            };
            try
            {
                System.Diagnostics.Process.Start(psi);
                Application.Current.Exit();
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    RequestedTheme=EgoTools.App.RootTheme,
                    Title = "提权失败",
                    Content = $"无法以管理员身份重启：{ex.Message}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}
