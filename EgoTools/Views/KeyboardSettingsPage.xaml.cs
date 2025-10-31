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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EgoTools.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class KeyboardSettingsPage : Page
    {
        private AppConfig config = default!;
        public KeyboardSettingsPage()
        {
            InitializeComponent();
            config = App.LoadConfig() ?? new AppConfig
            {
                KeyboardSettings = new KeyboardSettings { WirelessFunc = false },
                ColorManagement = new ColorManagement { CurrentMode = "Factory", CurrentProfile = "sRGB", IgcFile = "", _3dlutFile = "" },
                PowerThreshold = new BatteryControl { ChargeLimit = 100 }
            };
            if (config.KeyboardSettings == null)
            {
                config.KeyboardSettings = new KeyboardSettings { WirelessFunc = false };
            }
            bool detachment = config?.KeyboardSettings?.WirelessFunc ?? false;
            KeyboardDetachmentSwitch.IsOn = detachment;
            KeyboardDetachmentSwitch.Toggled += KeyboardDetachmentSwitch_Toggled;
        }
        private async void KeyboardDetachmentSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            // 进度弹窗
            var progressBar = new ProgressBar
            {
                IsIndeterminate = true,
                Width = 250,
                Height = 20,
                Margin = new Thickness(0, 16, 0, 0)
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = "正在切换键盘分离状态...", FontSize = 16, Margin = new Thickness(0,0,0,8) });
            stack.Children.Add(progressBar);
            var runningDialog = new ContentDialog
            {
                Title = "请稍候",
                Content = stack
            };
            runningDialog.XamlRoot = this.XamlRoot;
            var showTask = runningDialog.ShowAsync();
            var minDurationTask = System.Threading.Tasks.Task.Delay(1500);

            string exeName = System.IO.Path.Combine(AppContext.BaseDirectory, "utils", "kbd-detach.exe");
            string arg = KeyboardDetachmentSwitch.IsOn ? "enable" : "disable";
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exeName,
                    Arguments = arg,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = AppContext.BaseDirectory
                };
                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    await minDurationTask;
                    runningDialog.Hide();
                    if (process.ExitCode != 0)
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "键盘分离设置失败",
                            Content = $"命令行错误：{error}",
                            CloseButtonText = "确定",
                            XamlRoot = this.XamlRoot
                        };
                        await dialog.ShowAsync();
                        return;
                    }
                }
                // 命令执行成功保存json
                if (config?.KeyboardSettings != null)
                    config.KeyboardSettings.WirelessFunc = KeyboardDetachmentSwitch.IsOn;
                if (config != null)
                    App.SaveConfig(config);
            }
            catch (Exception ex)
            {
                await minDurationTask;
                runningDialog.Hide();
                var dialog = new ContentDialog
                {
                    Title = "调用失败",
                    Content = $"无法调用kbd-detach.exe：{ex.Message}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}
